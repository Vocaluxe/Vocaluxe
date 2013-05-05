using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using ClientServerLib;
using Vocaluxe.Base.Server;

namespace AndroidRemote
{
	[Activity (Label = "Vocaluxe Remote", MainLauncher = true)]
	public class Activity1 : Activity
	{
		CClient client = new CClient();
		Button bConnect;
		Button bLogin;
		Button bUp;
		Button bDown;
		bool loggedIn;
		
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			
			loggedIn = false;
			
			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);
			
			bConnect = FindViewById<Button> (Resource.Id.btConnect);
			bConnect.Click += delegate { Connect(); };
			
			bLogin = FindViewById<Button> (Resource.Id.btLogin);
			bLogin.Click += delegate { Login(); };
			
			bUp = FindViewById<Button> (Resource.Id.btUp);
			bUp.Click += delegate { 
				client.SendMessage (CCommands.CreateCommandWithoutParams (CCommands.CommandSendKeyUp), OnResponse); 
			};
			
			bDown = FindViewById<Button> (Resource.Id.btDown);
			bDown.Click += delegate { 
				client.SendMessage (CCommands.CreateCommandWithoutParams (CCommands.CommandSendKeyDown), OnResponse); 
			};
		}
		
		protected override void OnStop ()
		{
			base.OnStop();
			client.Close();
		}
		
		private void Connect()
		{
			if (!client.Connected)
				client.Connect("192.168.178.57", 3000, OnConnectionChanged);
			else
				client.Disconnect();
		}
		
		private void Login()
		{
			if (!loggedIn)
				client.SendMessage (CCommands.CreateCommandLogin("vocaluxe"), OnResponse);
			else
				client.Disconnect();
		}
		
		private void OnConnectionChanged(bool Connected)
		{
			RunOnUiThread (() => UpdateConnectionStatus(Connected));
		}
		
		private void OnResponse(byte[] Message)
		{
			if (Message == null)
				return;
			
			if (Message.Length < 4)
				return;
			
			int command = BitConverter.ToInt32(Message, 0);
			switch (command)
			{
			case CCommands.ResponseLoginFailed:
				RunOnUiThread (() => Toast.MakeText(this, "Login Failed", ToastLength.Short).Show());
				break;
				
			case CCommands.ResponseLoginWrongPassword:
				RunOnUiThread (() => Toast.MakeText(this, "Wrong Password", ToastLength.Short).Show());
				break;
				
			case CCommands.ResponseLoginOK:
				RunOnUiThread (() => UpdateLoginStatus(true));
				break;
				
			default:
				break;
			}
		}

		private void UpdateConnectionStatus(bool Connected)
		{
			if (Connected)
			{
				bLogin.Enabled = true;
				bConnect.Text = Resources.GetString(Resource.String.button_disconnect);
				Toast.MakeText(this, Resource.String.message_connected, ToastLength.Short).Show();
			}
			else
			{
				client.Disconnect();
				bLogin.Enabled = false;
				bLogin.Text = Resources.GetString(Resource.String.button_login);
				bConnect.Text = Resources.GetString(Resource.String.button_connect);
				bUp.Enabled = false;
				bDown.Enabled = false;
				Toast.MakeText(this, Resources.GetString(Resource.String.message_disconnected), ToastLength.Short).Show();
			}
		}
		
		private void UpdateLoginStatus(bool LoggedIn)
		{
			loggedIn = LoggedIn;

			if (LoggedIn)
			{
				bUp.Enabled = LoggedIn;
				bDown.Enabled = LoggedIn;
				bLogin.Text = Resources.GetString(Resource.String.button_logout);
				Toast.MakeText(this, "Logged In", ToastLength.Short).Show();
			}
			else
				Toast.MakeText(this, "Not Logged In", ToastLength.Short).Show();
		}
	}
}


