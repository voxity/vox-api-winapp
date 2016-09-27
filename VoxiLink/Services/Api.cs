
namespace VoxiLink.Services
{
    class Api
    {
        private static string token = "";

        private static Voxity.API.ApiSession session;

        public static Voxity.API.ApiSession Session
        {
            get
            {
                if (session == null)
                {
                    session = new Voxity.API.ApiSession(
                        VoxityApi.Default.CLIENT_ID,
                        VoxityApi.Default.CLIENT_SECRET,
                        VoxityApi.Default.HOST,
                        VoxityApi.Default.PORT,
                        accessToken: token,
                        refreshToken: VoxityApi.Default.REFRESH_TOKEN
                    );  
                }
                else
                {
                    if(token != session.AccessToken)
                    {
                        token = session.AccessToken;
                        VoxityApi.Default.Save();
                    }
                        

                    if (VoxityApi.Default.REFRESH_TOKEN != session.RefreshToken)
                    {
                        VoxityApi.Default.REFRESH_TOKEN = session.RefreshToken;
                        VoxityApi.Default.Save();
                    }
                }

                return session;
            }
        }

        private static Voxity.API.Models.User user;

        public static Voxity.API.Models.User User
        {
            get
            {
                if (user == null)
                {
                    user = session.Users.WhoAmI();
                }
                return user;
            }
        }
    }
}