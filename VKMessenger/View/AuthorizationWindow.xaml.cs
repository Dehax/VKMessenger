using System;
using System.Web;
using System.Windows;
using System.Windows.Navigation;
using VkNet.Enums.Filters;

namespace VKMessenger.View
{
	/// <summary>
	/// Окно авторизации.
	/// </summary>
	public partial class AuthorizationWindow : Window
    {
        private long APPLICATION_ID = 5570691;

        private string AUTHORIZE_URL = @"https://oauth.vk.com/authorize?client_id={0}&redirect_uri={1}&display=page&scope={2}&response_type=token&v=5.52&state=VKMessenger";
        private string REDIRECT_URI = @"https://oauth.vk.com/blank.html";

        public string AccessToken { get; private set; }
        public long UserId { get; private set; }

        public AuthorizationWindow(bool revoke = false)
        {
            InitializeComponent();
            
            string url = string.Format(AUTHORIZE_URL, APPLICATION_ID, REDIRECT_URI, Settings.Messages | Settings.Offline);
			if (revoke)
			{
				url += "&revoke=1";
			}
            browser.Source = new Uri(url);
            browser.LoadCompleted += browser_LoadCompleted;
        }

        private void browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            if (e.Uri.OriginalString.StartsWith(REDIRECT_URI))
            {
                if (!e.Uri.Fragment.Contains("error"))
                {
                    string cleanFragmentValue = e.Uri.Fragment.Substring(1);
                    string accessToken = HttpUtility.ParseQueryString(cleanFragmentValue).Get("access_token");
                    long userId = long.Parse(HttpUtility.ParseQueryString(cleanFragmentValue).Get("user_id"));
                    AccessToken = accessToken;
                    UserId = userId;
                    Close();
                }
            }
        }
    }
}
