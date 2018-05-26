using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

public class SysTrayApp : Form {

  [STAThread]
  public static void Main(string[] args) {
    Application.Run(new SysTrayApp(args.Length > 0 ? args[0] : ""));
  }

  enum Status {
    Unknown,
    Success,
    Failure,
    Error,
    Canceled,
  }

  enum Activity {
    Sleeping,
    Building
  }

  Status status = Status.Unknown;
  Activity activity = Activity.Sleeping;

  string url = "";
  string webUrl = "";

  Timer requestTimer = new Timer();

  private NotifyIcon trayIcon;
  private ContextMenu trayMenu;
  private string repoName = "";

  public SysTrayApp(string url) {
    this.url = url;

	Task.Run(() => {
	    XmlTextReader reader = new XmlTextReader(url);
		while (reader.Read()) {
		  if (reader.Name == "Project") {
			repoName = reader.GetAttribute("name");
			string activity = reader.GetAttribute("activity");
			string lastBuildStatus = reader.GetAttribute("lastBuildStatus");
			string lastBuildLabel = reader.GetAttribute("lastBuildLabel");
			string lastBuildTime = reader.GetAttribute("lastBuildTime");
			webUrl = reader.GetAttribute("webUrl");

			Console.WriteLine("name " + repoName);
			Console.WriteLine("activity " + activity);
			Console.WriteLine("lastBuildStatus " + lastBuildStatus);
			Console.WriteLine("lastBuildLabel " + lastBuildLabel);
			Console.WriteLine("lastBuildTime " + lastBuildTime);
			Console.WriteLine("webUrl " + webUrl);
			Console.WriteLine("");

			//SetState(lastBuildStatus, activity);
			return activity;
		  }
		}
		return "";
	});

    requestTimer.Tick += new EventHandler((s, e) => refreshStatus());

    // Sets the timer interval to 5 seconds.
    requestTimer.Interval = 5000;

    // Create a simple tray menu with only one item.
    trayMenu = new ContextMenu();
    trayMenu.MenuItems.Add("Exit", OnExit);

    // Create a tray icon. In this example we use a
    // standard system icon for simplicity, but you
    // can of course use your own custom icon too.
    trayIcon = new NotifyIcon();
    trayIcon.Text = "Loading...";
    trayIcon.Icon = Trayvis.Resources.hourglass;
    trayIcon.DoubleClick += (s, e) => {
      if (webUrl.Length != 0) {
        System.Diagnostics.Process.Start(webUrl);
      }
    };

    // Add menu to tray icon and show it.
    trayIcon.ContextMenu = trayMenu;
    trayIcon.Visible = true;

    refreshStatus();
    requestTimer.Start();
  }

  private void refreshStatus() {
    try {
      requestXml();
      refreshIcon();
    }
    catch (Exception e) {
      string text = "Error: " + e.Message;
      if (text.Length > 63) {
        //Windows doesnt like text longer than 63 characters for tray icons.
        text = text.Substring(0, 63);
      }
      trayIcon.Text = text;
      trayIcon.Icon = Trayvis.Resources.cancel;
    }
  }

  private void refreshIcon() {
    trayIcon.Text = repoName;

    switch (status) {
      case Status.Unknown:
        trayIcon.Icon = activity == Activity.Building ? Trayvis.Resources.hourglass_building : Trayvis.Resources.hourglass;
        break;
      case Status.Success:
        trayIcon.Icon = activity == Activity.Building ? Trayvis.Resources.application_xp_terminal_building : Trayvis.Resources.application_xp_terminal;
        break;
      case Status.Canceled:
        trayIcon.Icon = activity == Activity.Building ? Trayvis.Resources.bin_building : Trayvis.Resources.bin;
        break;
      case Status.Failure:
      case Status.Error:
        trayIcon.Icon = activity == Activity.Building ? Trayvis.Resources.bug_building : Trayvis.Resources.bug;
        break;
    }
  }

  private void requestXml() {
    XmlTextReader reader = new XmlTextReader(url);
    while (reader.Read()) {
      if (reader.Name == "Project") {
        repoName = reader.GetAttribute("name");
        string activity = reader.GetAttribute("activity");
        string lastBuildStatus = reader.GetAttribute("lastBuildStatus");
        string lastBuildLabel = reader.GetAttribute("lastBuildLabel");
        string lastBuildTime = reader.GetAttribute("lastBuildTime");
        webUrl = reader.GetAttribute("webUrl");

        Console.WriteLine("name " + repoName);
        Console.WriteLine("activity " + activity);
        Console.WriteLine("lastBuildStatus " + lastBuildStatus);
        Console.WriteLine("lastBuildLabel " + lastBuildLabel);
        Console.WriteLine("lastBuildTime " + lastBuildTime);
        Console.WriteLine("webUrl " + webUrl);
        Console.WriteLine("");

        SetState(lastBuildStatus, activity);
      }
    }
  }

  private void SetState(string status, string activity) {
    switch (status) {
      case "Unknown": /*this.status = Status.Unknown;*/ break; //just keep the last build status
      case "Success": this.status = Status.Success; break;
      case "Failure": this.status = Status.Failure; break;
      case "Error": this.status = Status.Error; break;
      case "Canceled": this.status = Status.Canceled; break;
      default: throw new Exception("Unknown status");
    }

    switch (activity) {
      case "Sleeping": this.activity = Activity.Sleeping; break;
      case "Building": this.activity = Activity.Building; break;
      default: throw new Exception("Unknown activity");
    }
  }

  protected override void OnLoad(EventArgs e) {
    Visible = false; // Hide form window.
    ShowInTaskbar = false; // Remove from taskbar.

    base.OnLoad(e);
  }

  private void OnExit(object sender, EventArgs e) {
    Application.Exit();
  }

  protected override void Dispose(bool isDisposing) {
    if (isDisposing) {
      // Release the icon resource.
      trayIcon.Dispose();
    }

    base.Dispose(isDisposing);
  }
}
