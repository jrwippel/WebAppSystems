using System.Collections.Concurrent;

namespace WebAppSystems.Services
{
    /// <summary>
    /// Serviço para controlar tentativas de login e prevenir ataques de força bruta
    /// </summary>
    public class LoginAttemptService
    {
        private readonly ConcurrentDictionary<string, LoginAttemptInfo> _attempts = new();
        private const int MaxAttempts = 5;
        private const int LockoutMinutes = 15;

        public class LoginAttemptInfo
        {
            public int FailedAttempts { get; set; }
            public DateTime? LockoutUntil { get; set; }
            public DateTime LastAttempt { get; set; }
        }

        public bool IsLockedOut(string login)
        {
            if (!_attempts.TryGetValue(login.ToLower(), out var info))
                return false;

            if (info.LockoutUntil.HasValue && info.LockoutUntil.Value > DateTime.UtcNow)
                return true;

            if (info.LockoutUntil.HasValue && info.LockoutUntil.Value <= DateTime.UtcNow)
            {
                _attempts.TryRemove(login.ToLower(), out _);
                return false;
            }

            return false;
        }

        public TimeSpan? GetLockoutTimeRemaining(string login)
        {
            if (!_attempts.TryGetValue(login.ToLower(), out var info))
                return null;

            if (info.LockoutUntil.HasValue && info.LockoutUntil.Value > DateTime.UtcNow)
                return info.LockoutUntil.Value - DateTime.UtcNow;

            return null;
        }

        public void RecordFailedAttempt(string login)
        {
            var key = login.ToLower();
            var info = _attempts.GetOrAdd(key, _ => new LoginAttemptInfo());

            info.FailedAttempts++;
            info.LastAttempt = DateTime.UtcNow;

            if (info.FailedAttempts >= MaxAttempts)
                info.LockoutUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
        }

        public void ResetAttempts(string login)
        {
            _attempts.TryRemove(login.ToLower(), out _);
        }

        public int GetRemainingAttempts(string login)
        {
            if (!_attempts.TryGetValue(login.ToLower(), out var info))
                return MaxAttempts;

            return Math.Max(0, MaxAttempts - info.FailedAttempts);
        }
    }
}
