using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace Xamarin.FindAllFiles
{
    public partial class FindOptionsViewController : AppKit.NSViewController
    {
        #region Constructors

        // Called when created from unmanaged code
        public FindOptionsViewController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public FindOptionsViewController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public FindOptionsViewController() : base("FindOptionsView", NSBundle.MainBundle)
        {
            Initialize();
        }

        // Shared initialization code
        void Initialize()
        {
        }

        #endregion

        public override void ViewDidLoad()
        {
            // TODO: no events plz
            // TODO: Probably don't need a button anyway, can do same behavior as vscode. ViewModel shouldn't care either way
            findButton.Activated += FindButton_Activated;
        }

        IFindResultsView findResultsView;

        private void FindButton_Activated(object sender, EventArgs args)
        {
            // TODO: Need cancellation and stuff
            findButton.Enabled = false;

            findResultsView = FindResultsViewController.CurrentFindResultsView;
            findResultsView.Clear();

            // TODO: Why do I (now) consistently get 335 results in 107 files for "monodevelop", but vscode gets 391 in 111? Clearly need to play with options
            //       My numbers at least match what I'm getting back from rg (can check summary if using --json)

            // TODO: When we have internet again, switch to use json output like vscode
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/local/bin/rg",
                    WorkingDirectory = "/Users/sandy/xam-git/monodevelop",
                    Arguments = $"\"{searchField.StringValue}\"", // TODO: Real escaping, etc
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                }
            };

            var sw = new Stopwatch();
            sw.Start();

            Task.Run(() =>
            {
                try
                {
                    p.Start();
                    p.BeginOutputReadLine();

                    var currentGroup = new List<FindResultViewModel>();
                    var currentGroupFilePath = string.Empty;

                    // TODO: Check if we guarantee to get full lines here. Actually just use mirepoix when you have internet
                    //       Looks like we get a line at a time.
                    p.OutputDataReceived += (o, e) =>
                    {
                        try
                        {
                            var filePath = string.Empty;
                            var data = string.Empty;

                            if (e.Data != null)
                            {

                                //Console.Error.WriteLine(e.Data);
                                var splitIndex = e.Data.IndexOf(':');
                                if (splitIndex < 0)
                                    return;

                                filePath = e.Data.Substring(0, splitIndex);
                                data = e.Data.Substring(splitIndex + 1).TrimStart();
                            }

                            if (currentGroupFilePath != string.Empty && filePath != currentGroupFilePath)
                            {
                                var resultsToPush = new[] {
                                    new FindResultGroupViewModel(
                                        Path.GetFileName(currentGroupFilePath),
                                        Path.GetDirectoryName(currentGroupFilePath),
                                        currentGroup),
                                };

                                BeginInvokeOnMainThread(() =>
                                    {
                                        try
                                        {
                                            findResultsView.PushResults(resultsToPush);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.Error.WriteLine(ex);
                                        }
                                    });

                                currentGroup = new List<FindResultViewModel>();
                                currentGroupFilePath = filePath;
                            }
                            else if (currentGroupFilePath == string.Empty)
                            {
                                currentGroupFilePath = filePath;
                            }

                            if (data != string.Empty)
                                currentGroup.Add(new FindResultViewModel(data, 0, 0));
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(ex);
                        }
                    };

                    p.WaitForExit();
                    sw.Stop();

                    Console.Error.WriteLine($"Completed in {sw.ElapsedMilliseconds}ms");

                    BeginInvokeOnMainThread(() =>
                    {
                        findResultsView.EndSearch(sw.Elapsed);
                        findButton.Enabled = true;
                    });
                    
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            });
        }
    }
}
