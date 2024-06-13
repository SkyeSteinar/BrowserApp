using Microsoft.Web.WebView2.Core;
using System;
using System.Linq;
using System.Windows.Forms;

namespace BrowserApp
{
    public partial class BrowserForm : Form
    {
        bool _extraDebug = false;

        public BrowserForm()
        {
            InitializeComponent();
            InitializeWebBrowser();
        }

        private void InitializeWebBrowser()
        {
            webBrowser.CoreWebView2InitializationCompleted += (sender, args) =>
            {
                if (args.IsSuccess)
                {
                    webBrowser.CoreWebView2.WebResourceRequested += webBrowser_WebResourceRequested;
                    webBrowser.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                }
            };
        }

        private async void btnGo_Click(object sender, EventArgs e)
        {
            await webBrowser.EnsureCoreWebView2Async();
            webBrowser.NavigationStarting += WebBrowser_NavigationStarting;
            webBrowser.Source = new Uri("https://auth.mobile.apps.pilot.banedata.utv.banenor.no/login/#/form");
            if (_extraDebug) webBrowser.CoreWebView2.OpenDevToolsWindow();
        }

        private void btnQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void WebBrowser_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            string uri = e.Uri;
            CoreWebView2NavigationKind kind = e.NavigationKind;
            Console.WriteLine($"uri: {uri}\nkind: {kind.ToString()}");
        }

        private void webBrowser_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (e.ResourceContext == CoreWebView2WebResourceContext.Document)
            {
                CoreWebView2HttpRequestHeaders requestHeaders = e.Request.Headers;
                string oauthToken = ExtractOAuthTokenFromCookieHeader(requestHeaders.FirstOrDefault(x => x.Key == "Cookie").Value);
                if (requestHeaders.Contains("Authorization"))
                {
                    string authHeader = requestHeaders.FirstOrDefault(x => x.Key == "Authorization").Value;
                    Console.WriteLine($"OAuth Authorization header: {authHeader}");
                }
                else if (requestHeaders.Contains("Cookie") && oauthToken != string.Empty)
                {
                    Console.WriteLine($"OAuth cookie token: {oauthToken}");
                }
                else
                {
                    string headers = "";
                    foreach (var item in requestHeaders)
                    {
                        if (_extraDebug && item.Key.Equals("Cookie"))
                            headers += $"{item.Key}={item.Value},";
                        else
                            headers += $"{item.Key}, ";
                    }
                    Console.WriteLine($"Headers: {headers}");
                }
            }
        }
        private string ExtractOAuthTokenFromCookieHeader(string cookieHeader)
        {
            if (cookieHeader == null)
                return null;

            string tokenName = "x-access-token=";
            int tokenStart = cookieHeader.IndexOf(tokenName);
            if (tokenStart != -1)
            {
                int tokenEnd = cookieHeader.IndexOf(';', tokenStart);
                if (tokenEnd != -1) tokenEnd = cookieHeader.Length;
                return cookieHeader.Substring(tokenStart + tokenName.Length, tokenEnd - (tokenStart + tokenName.Length));
            }
            return string.Empty;
        }

    }
}
