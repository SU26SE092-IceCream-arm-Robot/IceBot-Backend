using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;

namespace Infrastructure.Firebase;

public interface IFirebaseClient
{
    FirebaseAuth GetAuth();
    FirebaseMessaging GetMessaging();
}
