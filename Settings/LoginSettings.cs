using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace FollowBot.Settings
{
    public class LoginSettings : INotifyPropertyChanged
    {
        private bool _autoLoginEnabled = false;
        private string _characterName = "";
        private string _email = "";
        private string _encryptedPassword = "";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DefaultValue(false)]
        public bool AutoLoginEnabled
        {
            get => _autoLoginEnabled;
            set
            {
                _autoLoginEnabled = value;
                OnPropertyChanged(nameof(AutoLoginEnabled));
            }
        }

        /// <summary>
        /// If set, selects this character by exact name.
        /// If empty, uses smart selection (filters out Standard/Phrecia/Void/private leagues).
        /// </summary>
        [DefaultValue("")]
        public string CharacterName
        {
            get => _characterName;
            set
            {
                _characterName = value;
                OnPropertyChanged(nameof(CharacterName));
            }
        }

        /// <summary>
        /// Stored as plaintext (already visible on PoE's login screen).
        /// </summary>
        [DefaultValue("")]
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        /// <summary>
        /// DPAPI-encrypted password stored as Base64. Tied to the Windows user account.
        /// </summary>
        [DefaultValue("")]
        public string EncryptedPassword
        {
            get => _encryptedPassword;
            set
            {
                _encryptedPassword = value;
                OnPropertyChanged(nameof(EncryptedPassword));
            }
        }

        /// <summary>
        /// Encrypts the plaintext password with DPAPI and stores it in EncryptedPassword as Base64.
        /// </summary>
        public void SetAndEncryptPassword(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
            {
                EncryptedPassword = "";
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(plaintext);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            EncryptedPassword = Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// Decrypts the stored DPAPI password. Returns empty string if no password is stored.
        /// </summary>
        public string DecryptPassword()
        {
            if (string.IsNullOrEmpty(_encryptedPassword))
                return "";

            try
            {
                var encrypted = Convert.FromBase64String(_encryptedPassword);
                var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch (CryptographicException)
            {
                // DPAPI data was encrypted by a different user or is corrupted
                return "";
            }
        }

        /// <summary>
        /// Returns true if a password has been stored.
        /// </summary>
        [JsonIgnore]
        public bool HasPassword => !string.IsNullOrEmpty(_encryptedPassword);
    }
}
