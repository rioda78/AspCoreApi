using Microsoft.AspNetCore.Identity;

namespace AspCoreApi.Ekstensi
{
    public class DummyEmailSender<TUser> : IEmailSender<TUser> where TUser : class
    {
        public Task SendConfirmationLinkAsync(TUser user, string email, string confirmationLink) =>
            Task.CompletedTask;

        public Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink) =>
            Task.CompletedTask;

        public Task SendPasswordResetCodeAsync(TUser user, string email, string resetCode) =>
            Task.CompletedTask;

        public Task SendVerificationCodeAsync(TUser user, string email, string verificationCode) =>
            Task.CompletedTask;
    }

}
