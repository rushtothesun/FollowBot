using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using FollowBot.SimpleEXtensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace FollowBot.Tasks
{
    public class AutoLoginTask : ITask
    {
        // Volatile state — NOT stored in settings
        private static readonly Stopwatch _loginAttemptTimer = new Stopwatch();
        private static int _loginRetryCount;
        private static bool _passwordEntryRequired;
        private const int MaxLoginRetries = 5;
        private const int PopupDetectionTimeoutMs = 5000;

        public string Author => "FollowBot";
        public string Description => "Automatically logs in and selects a character.";
        public string Name => "AutoLogin";
        public string Version => "1.0.0.0";

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        public async Task<bool> Run()
        {
            var settings = FollowBotSettings.Instance.Login;

            // Master toggle check
            if (!settings.AutoLoginEnabled)
                return false;

            // Already in game — nothing to do
            if (LokiPoe.IsInGame)
            {
                ResetState();
                return false;
            }

            // Handle login screen
            if (LokiPoe.IsInLoginScreen)
            {
                return await HandleLoginScreen(settings);
            }

            // Handle character selection screen
            if (LokiPoe.IsInCharacterSelectionScreen)
            {
                return await HandleCharacterSelection(settings);
            }

            return false;
        }

        private async Task<bool> HandleLoginScreen(Settings.LoginSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Email) || string.IsNullOrEmpty(settings.DecryptPassword()))
            {
                GlobalLog.Error($"[{Name}] Email or password not configured. Cannot login.");
                GlobalLog.Error($"[{Name}] Please set credentials in FollowBot General settings or disable Auto Login.");
                BotManager.Stop();
                return true;
            }

            // Check if we've exceeded retry limit
            if (_loginRetryCount >= MaxLoginRetries)
            {
                GlobalLog.Error($"[{Name}] Max login retries ({MaxLoginRetries}) reached. Stopping. Check credentials or network.");
                BotManager.Stop();
                return true;
            }

            // If connecting, just wait
            if (LokiPoe.LoginState.IsConnecting)
            {
                GlobalLog.Debug($"[{Name}] Login is connecting, waiting...");
                await Coroutine.Sleep(LokiPoe.Random.Next(800, 1200));
                return true;
            }

            // If we need to enter password (popup was detected previously)
            if (_passwordEntryRequired)
            {
                return await HandlePasswordEntry(settings);
            }

            // First attempt: try Login() with currently selected gateway (uses PoE's "remember me")
            var gateway = LokiPoe.LoginState.CurrentSelectedGateway;
            GlobalLog.Debug($"[{Name}] Attempting login with gateway: {gateway} (attempt {_loginRetryCount + 1}/{MaxLoginRetries})");

            //Login() crashes with ArgumentOutOfRangeException at AcceptTermOfUse()
            LokiPoe.LoginState.LoginError loginResult;
            try
            {
                loginResult = LokiPoe.LoginState.Login(gateway);
            }
            catch (ArgumentOutOfRangeException)
            {
                GlobalLog.Warn($"[{Name}] Popup Detected, Handling.");
                _loginRetryCount++;
                _passwordEntryRequired = true;
                await DismissPopup();
                return true;
            }

            _loginRetryCount++;
            _loginAttemptTimer.Restart();

            if (loginResult == LokiPoe.LoginState.LoginError.None)
            {
                GlobalLog.Debug($"[{Name}] Login() returned None — waiting for transition...");
                await Coroutine.Sleep(LokiPoe.Random.Next(1900, 2300));
                return true;
            }

            if (loginResult == LokiPoe.LoginState.LoginError.InQueue)
            {
                GlobalLog.Info($"[{Name}] In login queue, waiting patiently...");
                await Coroutine.Sleep(LokiPoe.Random.Next(4800, 5300));
                return true;
            }

            if (loginResult == LokiPoe.LoginState.LoginError.LoginErrorWindowPresent)
            {
                // There's an error popup — dismiss it and try with explicit credentials
                GlobalLog.Warn($"[{Name}] Login error window detected. Dismissing popup...");
                _passwordEntryRequired = true;
                await DismissPopup();
                return true;
            }

            if (loginResult == LokiPoe.LoginState.LoginError.NoCredentials)
            {
                // No remembered credentials — need to enter them
                GlobalLog.Info($"[{Name}] No saved credentials. Will enter email/password...");
                _passwordEntryRequired = true;
                await Coroutine.Sleep(LokiPoe.Random.Next(400, 700));
                return true;
            }

            // Any other error
            GlobalLog.Warn($"[{Name}] Login returned: {loginResult}. Retrying after delay...");
            await Coroutine.Sleep(LokiPoe.Random.Next(2800, 3300));
            return true;
        }

        private async Task<bool> HandlePasswordEntry(Settings.LoginSettings settings)
        {
            var email = settings.Email;
            var password = settings.DecryptPassword();

            await Coroutine.Sleep(LokiPoe.Random.Next(400, 700));

            LokiPoe.LoginState.SetEmail(email);
            await Coroutine.Sleep(LokiPoe.Random.Next(600, 800));
            LokiPoe.LoginState.SetPassword(password);

            await Coroutine.Sleep(LokiPoe.Random.Next(600, 800));

            var gateway = LokiPoe.LoginState.CurrentSelectedGateway;
            var loginResult = LokiPoe.LoginState.Login(gateway);
            _loginRetryCount++;

            if (loginResult == LokiPoe.LoginState.LoginError.None)
            {
                GlobalLog.Debug($"[{Name}] Login with credentials succeeded — waiting for transition...");
                _passwordEntryRequired = false;
                await Coroutine.Sleep(LokiPoe.Random.Next(2900, 3300));
                return true;
            }

            GlobalLog.Warn($"[{Name}] Login with credentials returned: {loginResult}");
            await Coroutine.Sleep(LokiPoe.Random.Next(2900, 3300));
            return true;
        }

        private async Task DismissPopup()
        {
            // Key down to dismiss popup
            LokiPoe.Input.SimulateKeyEvent(Keys.Return, true, false, false, Keys.None);
            await Coroutine.Sleep(LokiPoe.Random.Next(800, 1200));
        }

        private async Task<bool> HandleCharacterSelection(Settings.LoginSettings settings)
        {
            // Wait for character list to load
            if (!LokiPoe.SelectCharacterState.IsCharacterListLoaded)
            {
                GlobalLog.Debug($"[{Name}] Character list not yet loaded, waiting...");
                await Coroutine.Sleep(LokiPoe.Random.Next(800, 1200));
                return true;
            }

            var characters = LokiPoe.SelectCharacterState.Characters;
            if (characters == null)
            {
                GlobalLog.Error($"[{Name}] Character list is null.");
                await Coroutine.Sleep(LokiPoe.Random.Next(1900, 2300));
                return true;
            }

            string targetName;

            // If a specific character is configured, use it directly
            if (!string.IsNullOrWhiteSpace(settings.CharacterName))
            {
                targetName = settings.CharacterName.Trim();
                GlobalLog.Info($"[{Name}] Using configured character name: {targetName}");
            }
            else
            {
                // Smart selection: filter out Standard, Phrecia, Void, private leagues
                var filtered = characters.Where(c =>
                    !c.League.Contains("Standard") &&
                    !c.League.Contains("Phrecia") &&
                    !c.League.Contains("Void") &&
                    !c.League.Contains("(") // private leagues like "Atlas Invasion (Numbers)"
                ).ToList();

                if (filtered.Count == 0)
                {
                    GlobalLog.Error($"[{Name}] No current league character found. All characters are in Standard/Phrecia/Void/private leagues. Set a character name manually in settings.");
                    BotManager.Stop();
                    return true;
                }

                if (filtered.Count > 1)
                {
                    var names = string.Join(", ", filtered.Select(c => $"{c.Name} (Lv{c.Level} {c.League})"));
                    GlobalLog.Warn($"[{Name}] Multiple current league characters found: {names}. Using first alphabetically. Consider setting a CharacterName in settings.");
                }

                targetName = filtered.First().Name;
                GlobalLog.Info($"[{Name}] Smart selection chose: {targetName}");
            }

            // Select the character
            var result = LokiPoe.SelectCharacterState.SelectCharacter(targetName);
            if (result == LokiPoe.SelectCharacterState.SelectCharacterError.None)
            {
                GlobalLog.Info($"[{Name}] Successfully selected character: {targetName}. Entering game...");
                ResetState();
                await Coroutine.Sleep(LokiPoe.Random.Next(4900, 5300)); // Wait for game load
                return true;
            }

            GlobalLog.Error($"[{Name}] Failed to select character '{targetName}': {result}");
            await Coroutine.Sleep(LokiPoe.Random.Next(2900, 3300));
            return true;
        }

        private void ResetState()
        {
            _loginRetryCount = 0;
            _passwordEntryRequired = false;
            _loginAttemptTimer.Reset();
        }

        public void Start()
        {
            ResetState();
            GlobalLog.Info($"[{Name}] Task Loaded.");
        }

        public void Stop()
        {
            ResetState();
        }

        public void Tick()
        {
            // If we're on the login screen and waiting for a response, check for popup timeout
            if (LokiPoe.IsInLoginScreen && _loginAttemptTimer.IsRunning &&
                _loginAttemptTimer.ElapsedMilliseconds > PopupDetectionTimeoutMs &&
                !LokiPoe.LoginState.IsConnecting &&
                !_passwordEntryRequired)
            {
                // Still on login screen after timeout and not connecting — popup likely present
                GlobalLog.Warn($"[{Name}] Still on login screen after {PopupDetectionTimeoutMs}ms. Popup detected.");
                _passwordEntryRequired = true;
                _loginAttemptTimer.Reset();
            }
        }
    }
}
