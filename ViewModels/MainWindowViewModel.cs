using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using TAnalyzer.Services;

namespace TAnalyzer.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string _selectedFilePath = "";

    private string
        _status = "WaitingForSource"; // Possible Statuses: 'ReadyForTranscript', "ReadyForReport, 'WaitingForSource', "Processing"


    // Properties
    public bool IsRecording { get; set; } // Bind to stop button enabled state

    private string _statusMessage = "STATUS: Waiting for an audio source...";

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    private string _transcriptionOutput = "";

    public string TranscriptionOutput
    {
        get => _transcriptionOutput;
        set => SetField(ref _transcriptionOutput, value);
    }


    // Controls whether the buttons are active or not
    private bool _canGenerateReport = false;

    public bool CanGenerateReport
    {
        get => _canGenerateReport;
        set => SetField(ref _canGenerateReport, value);
    }

    private bool _canGenerateTranscript = false;

    public bool CanGenerateTranscript
    {
        get => _canGenerateTranscript;
        set => SetField(ref _canGenerateTranscript, value);
    }

    private bool _canSelectFile = true;

    public bool CanSelectFile
    {
        get => _canSelectFile;
        set => SetField(ref _canSelectFile, value);
    }


    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set => SetField(ref _selectedFilePath, value);
    }

    // Update the status and message in 1
    public string Status
    {
        get => _status;
        set
        {
            //Update the message and bools accordingly
            switch (value)
            {
                case "WaitingForSource":
                    StatusMessage = "STATUS: Waiting for an audio source...";
                    CanGenerateReport = false;
                    CanGenerateTranscript = false;
                    CanSelectFile = true;
                    break;
                case "ReadyForTranscript":
                    StatusMessage = "STATUS: Transcript can be generated...";
                    CanGenerateTranscript = true;
                    CanGenerateReport = false;
                    CanSelectFile = true;
                    break;
                case "ReadyForReport":
                    StatusMessage = "STATUS: Report can be generated...";
                    CanGenerateTranscript = false;
                    CanGenerateReport = true;
                    CanSelectFile = true;
                    break;
                case "Processing":
                    StatusMessage = "STATUS: Processing..";
                    CanGenerateTranscript = false;
                    CanGenerateReport = false;
                    CanSelectFile = false;
                    break;
                default:
                    StatusMessage = "STATUS: Invalid! (report as bug)";
                    break;
            }

            // SetField because it triggers an update event
            SetField(ref _status, value);
        }
    }

   
    public bool CanGenerate => !string.IsNullOrEmpty(SelectedFilePath);

    // Commands
    public ICommand SelectRecordingCommand { get; }
    public ICommand GenerateCommand { get; }
    public ICommand StartRecordingCommand { get; }
    public ICommand StopRecordingCommand { get; }
    public ICommand GenerateTranscriptCommand { get; }
    public ICommand GenerateReportCommand { get; }

    private readonly SpeechTranscriptionService _transcriptionService;

    public MainWindowViewModel()
    {
        // Assigning Commands to functions
        SelectRecordingCommand = new RelayCommand(SelectRecording);
        StartRecordingCommand = new RelayCommand(StartRecording);
        StopRecordingCommand = new RelayCommand(StopRecording);
        GenerateTranscriptCommand = new AsyncRelayCommand(GenerateTranscriptionAsync);
        GenerateReportCommand = new AsyncRelayCommand(GenerateReportAsync);
        GenerateCommand = new RelayCommand(GenerateAnalysis, () => CanGenerate);

        //Creating the transcription service using Keys/Region from appsettings.json
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        _transcriptionService = new SpeechTranscriptionService(
            config["AzureSpeech:Key"],
            config["AzureSpeech:Region"]);

        Console.WriteLine(config["AzureSpeech:Key"] + " : " + config["AzureSpeech:Region"]);
    }

    private async void SelectRecording()
    {
        var dialog = new OpenFileDialog()
        {
            Title = "Select Audio File",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "WAV Files", Extensions = new List<string> { "wav" } },
                new FileDialogFilter { Name = "MP3 Files", Extensions = new List<string> { "mp3" } },
                new FileDialogFilter { Name = "All Files", Extensions = new List<string> { "*" } }
            },
            AllowMultiple = false
        };

        var result = await dialog.ShowAsync(new Window());

        // Runs if the selected path exists and is valid
        if (result != null && result.Length > 0)
        {
            // Change status
            Status = "ReadyForTranscript";
            SelectedFilePath = result[0];
            Console.WriteLine("Path:" + SelectedFilePath);
        }
    }

    private void GenerateAnalysis()
    {
        Status = "Analyzing...";
        // Call Python script and Google Cloud (same as before)
    }

    private void StartRecording()
    {
        // NO-OP
    }

    private void StopRecording()
    {
        //NO-OP
    }

    // private async Task GenerateTranscriptionAsync()
    // {
    //     if (string.IsNullOrWhiteSpace(SelectedFilePath)) return;
    //
    //     try
    //     {
    //         Status = "Processing";
    //         TranscriptionOutput = "Starting transcription...\n";
    //
    //         var result = await _transcriptionService.TranscribeConversationAsync(
    //             SelectedFilePath,
    //             update => TranscriptionOutput += update + "\n");
    //
    //         TranscriptionOutput += "\nTranscription complete!\n";
    //     }
    //     catch (Exception ex)
    //     {
    //         TranscriptionOutput = $"Error: {ex.Message}";
    //     }
    //     finally
    //     {
    //         Status = "WaitingForSource";
    //     }
    // }

    public async Task<string> GenerateTranscriptionAsync()
    {
        
        // Change status
        Status = "Processing";
        
        var tcs = new TaskCompletionSource<string>();
    
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"SpeechTranscriber.py {_selectedFilePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            },
            EnableRaisingEvents = true
        };
    
        StringBuilder output = new StringBuilder();
    
        process.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data) && !e.Data.Contains("NOMATCH"))
            {
                
                output.AppendLine(e.Data);
                TranscriptionOutput += e.Data + Environment.NewLine;
            }

        };
    
        process.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
                output.AppendLine($"ERROR: {e.Data}");
        };
    
        process.Exited += (sender, e) => {
            tcs.SetResult(output.ToString());
            process.Dispose();
            
            // Reset Status
            Status = "ReadyForReport";
        };
        
        
    
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    
        return await tcs.Task;
    }

    public async Task<string> GenerateReportAsync()
    {
        // Change status
        Status = "Processing";

        // Clear the output textblock
        string inputTranscript = new string(TranscriptionOutput);
        TranscriptionOutput = string.Empty;
        
        
        var tcs = new TaskCompletionSource<string>();
    
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"ReportGenerator.py \"{inputTranscript}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            },
            EnableRaisingEvents = true
        };
    
        StringBuilder output = new StringBuilder();
        
        
    
        process.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data) && !e.Data.Contains("NOMATCH"))
            {
                
                output.AppendLine(e.Data);
                TranscriptionOutput += e.Data + Environment.NewLine;
            }

        };
    
        process.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
                output.AppendLine($"ERROR: {e.Data}");
        };
    
        process.Exited += (sender, e) => {
            tcs.SetResult(output.ToString());
            process.Dispose();
            
            // Reset Status
            Status = "WaitingForSource";
        };
        
        
    
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    
        return await tcs.Task;
    }
}