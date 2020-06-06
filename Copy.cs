using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Task = System.Threading.Tasks.Task;

namespace Plagin
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Copy
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("2d5085df-50c7-4181-96d5-a8852638f65b");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Copy"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Copy(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
           commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Copy Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        
        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new Copy(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            String UKey;
            String PCode="api_paste_code=";
            String EDate;
            String PPrivate;
            String PName="";
            String PFormat;
            String FilePath;
            String Code;
            String POption = "&api_option=paste";
            if (CanFilesBeCompared(dte, out FilePath)) {
                using (StreamReader sr = new StreamReader(FilePath))
                {
                    Code = sr.ReadToEnd();
                }
                WebRequest request = WebRequest.Create("https://pastebin.com/api/api_post.php");
                request.Method = "POST";
                using (StreamReader sr = new StreamReader("C:\\Users\\Tim\\source\\repos\\Plagin\\UserSettings.txt"))
                {
                    UKey = (sr.ReadLine());
                    EDate =  (sr.ReadLine());
                    PPrivate = (sr.ReadLine());
                    PFormat = (sr.ReadLine());
                }
                Console.WriteLine("After reading user settings");
                int i = FilePath.Length - 1;
                while (i >= 0 && FilePath[i] != '\\')
                {
                    PName += FilePath[i];
                    i--;
                }
                String data = UKey + POption + "&" + PCode + Code + "&" + EDate + "&" + PPrivate + "&api_paste_name=" + PName + "&" + PFormat;
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(data);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }
                WebResponse response =  request.GetResponse();
                string responce;
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        responce = (String)reader.ReadToEnd().ToString();
                        Clipboard.SetText(responce);
                        MessageBox.Show( responce , "Сообщение", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    }
                }
                response.Close();
            }
           
        }
        public static IEnumerable<string> GetSelectedFile(EnvDTE80.DTE2 dte)
        {
            var items = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;
            return from item in items.Cast<UIHierarchyItem>()
                   let pi = item.Object as ProjectItem
                   select pi.FileNames[1];
        }
        private static bool CanFilesBeCompared(DTE2 dte, out string file)
        {
            var item = GetSelectedFile(dte);
            file = item.ElementAtOrDefault(0);
            var fileTest = item.ElementAtOrDefault(1);
            return !string.IsNullOrEmpty(file) && string.IsNullOrEmpty(fileTest);
        }
    }
}
