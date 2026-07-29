using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;

namespace Infrastructure.Firebase;

public interface IFirebaseClient
{
    Task<FirebaseToken> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
    Task<UserRecord> GetUserAsync(string uid, CancellationToken cancellationToken = default);
    FirebaseMessaging GetMessaging();
}
