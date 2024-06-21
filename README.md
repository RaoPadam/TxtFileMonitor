TxtFileMonitor

Overview
The TxtFileMonitor is a console application developed in C#. This program is designed to monitor changes in a specified .txt file and log these changes into a separate log file. 
By leveraging the FileSystemWatcher class, it keeps an eye on the target file and the DiffPlex library is used to calculate and display the differences between the old and new content. 
This tool is useful for tracking modifications in text files, making it ideal for scenario like monitoring log files.

Features:

File Monitoring: Tracks changes in a specified .txt file.
Change Logging: Logs any changes with timestamps into a separate log file.
Batch Reporting: Reports changes in batches every 15 seconds to ensure efficiency.
Debouncing: Avoids processing changes too frequently by implementing debouncing.
Change Detection: Utilizes the DiffPlex library to detect and report differences in file content effectively.

Usage Instructions

1.Start the Program: Run the executable (TxtFileMonitor.exe on Windows) from the command line or terminal.
2.Enter the File Path: When prompted, enter the path of the .txt file you want to monitor.
3.Enter the Log File Path: Enter the path for the log file. If the file does not exist, it will be created.
4.View Initial Content: The program will display the initial content of the file.
5.Monitor Changes: The program will start monitoring the file for changes. Changes will be logged and displayed every 15 seconds.
6.Exit Application: To stop the application, type "exit" and press enter.
