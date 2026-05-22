using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static string recordFile = "records.txt";
    static string auditFile = "audit.txt";

    static void Main()
    {
        InitializeStorage();

        while (true)
        {
            Console.WriteLine("\n===== STUDENT CONSULTATION LOG SYSTEM =====");
            Console.WriteLine("[1] Add Record");
            Console.WriteLine("[2] View Records");
            Console.WriteLine("[3] Search Record");
            Console.WriteLine("[4] Update Record");
            Console.WriteLine("[5] Delete Record (Soft Delete)");
            Console.WriteLine("[6] Generate Report");
            Console.WriteLine("[7] Exit");
            Console.Write("Choose: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1": AddRecord(); break;
                case "2": ViewRecords(); break;
                case "3": SearchRecord(); break;
                case "4": UpdateRecord(); break;
                case "5": DeleteRecord(); break;
                case "6": GenerateReport(); break;
                case "7": return;
                default:
                    Console.WriteLine("Invalid choice.");
                    LogAction("ERROR", "Invalid menu choice");
                    break;
            }
        }
    }

    // Initialize files
    static void InitializeStorage()
    {
        try
        {
            if (!File.Exists(recordFile))
                File.Create(recordFile).Close();

            if (!File.Exists(auditFile))
                File.Create(auditFile).Close();

            LogAction("INIT", "Storage initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error initializing storage: " + ex.Message);
        }
    }

    // Add Record
    static void AddRecord()
    {
        try
        {
            Console.Write("Enter Student Name: ");
            string name = Console.ReadLine();

            Console.Write("Enter Course: ");
            string course = Console.ReadLine();

            Console.Write("Enter Concern: ");
            string concern = Console.ReadLine();

            Console.Write("Enter Consultation Date (YYYY-MM-DD): ");
            string date = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(course) ||
                string.IsNullOrWhiteSpace(concern) ||
                string.IsNullOrWhiteSpace(date))
            {
                Console.WriteLine("Invalid input. Fields cannot be empty.");
                LogAction("ERROR", "Add failed due to empty input");
                return;
            }

            int id = GetNextId();
            string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string updatedAt = createdAt;
            bool isActive = true;
            string checksum = ComputeChecksum(name, course, concern, date);

            string record = $"{id}|{name}|{course}|{concern}|{date}|{createdAt}|{updatedAt}|{isActive}|{checksum}";
            File.AppendAllText(recordFile, record + Environment.NewLine);

            Console.WriteLine("Record added successfully.");
            LogAction("ADD", $"Record ID {id} added");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error adding record: " + ex.Message);
            LogAction("ERROR", ex.Message);
        }
    }

    // View Records
    static void ViewRecords()
    {
        try
        {
            string[] lines = File.ReadAllLines(recordFile);

            Console.WriteLine("\n===== ACTIVE RECORDS =====");

            foreach (string line in lines)
            {
                string[] parts = line.Split('|');

                if (parts.Length == 9 && parts[7] == "True")
                {
                    Console.WriteLine($"ID: {parts[0]}");
                    Console.WriteLine($"Name: {parts[1]}");
                    Console.WriteLine($"Course: {parts[2]}");
                    Console.WriteLine($"Concern: {parts[3]}");
                    Console.WriteLine($"Date: {parts[4]}");
                    Console.WriteLine("------------------------");
                }
            }

            LogAction("READ", "Viewed records");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error viewing records: " + ex.Message);
            LogAction("ERROR", ex.Message);
        }
    }

    // Search Record
    static void SearchRecord()
    {
        Console.Write("Enter student name to search: ");
        string search = Console.ReadLine().ToLower();

        string[] lines = File.ReadAllLines(recordFile);
        bool found = false;

        foreach (string line in lines)
        {
            string[] parts = line.Split('|');

            if (parts.Length == 9 &&
                parts[1].ToLower().Contains(search) &&
                parts[7] == "True")
            {
                Console.WriteLine($"ID: {parts[0]} | Name: {parts[1]} | Course: {parts[2]} | Concern: {parts[3]}");
                found = true;
            }
        }

        if (!found)
            Console.WriteLine("No record found.");

        LogAction("READ", $"Searched for {search}");
    }

    // Update Record
    static void UpdateRecord()
    {
        Console.Write("Enter Record ID to update: ");
        string idInput = Console.ReadLine();

        string[] lines = File.ReadAllLines(recordFile);
        List<string> updatedLines = new List<string>();
        bool found = false;

        foreach (string line in lines)
        {
            string[] parts = line.Split('|');

            if (parts.Length == 9 && parts[0] == idInput)
            {
                found = true;

                Console.Write("New Concern: ");
                string newConcern = Console.ReadLine();

                Console.Write("New Consultation Date: ");
                string newDate = Console.ReadLine();

                parts[3] = newConcern;
                parts[4] = newDate;
                parts[6] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                parts[8] = ComputeChecksum(parts[1], parts[2], newConcern, newDate);

                updatedLines.Add(string.Join("|", parts));

                Console.WriteLine("Record updated.");
                LogAction("UPDATE", $"Record ID {idInput} updated");
            }
            else
            {
                updatedLines.Add(line);
            }
        }

        if (!found)
        {
            Console.WriteLine("Record not found.");
            LogAction("ERROR", $"Update failed. ID {idInput} not found");
        }

        File.WriteAllLines(recordFile, updatedLines);
    }

    // Soft Delete
    static void DeleteRecord()
    {
        Console.Write("Enter Record ID to delete: ");
        string idInput = Console.ReadLine();

        string[] lines = File.ReadAllLines(recordFile);
        List<string> updatedLines = new List<string>();
        bool found = false;

        foreach (string line in lines)
        {
            string[] parts = line.Split('|');

            if (parts.Length == 9 && parts[0] == idInput)
            {
                found = true;
                parts[7] = "False"; // soft delete
                updatedLines.Add(string.Join("|", parts));

                Console.WriteLine("Record soft deleted.");
                LogAction("DELETE", $"Record ID {idInput} soft deleted");
            }
            else
            {
                updatedLines.Add(line);
            }
        }

        if (!found)
        {
            Console.WriteLine("Record not found.");
            LogAction("ERROR", $"Delete failed. ID {idInput} not found");
        }

        File.WriteAllLines(recordFile, updatedLines);
    }

    // Report
    static void GenerateReport()
    {
        string[] lines = File.ReadAllLines(recordFile);

        int total = 0;
        int active = 0;
        int deleted = 0;

        Dictionary<string, int> courseCount = new Dictionary<string, int>();

        foreach (string line in lines)
        {
            string[] parts = line.Split('|');

            if (parts.Length == 9)
            {
                total++;

                if (parts[7] == "True")
                    active++;
                else
                    deleted++;

                string course = parts[2];

                if (courseCount.ContainsKey(course))
                    courseCount[course]++;
                else
                    courseCount[course] = 1;
            }
        }

        Console.WriteLine("\n===== REPORT =====");
        Console.WriteLine("Total Records: " + total);
        Console.WriteLine("Active Records: " + active);
        Console.WriteLine("Deleted Records: " + deleted);

        Console.WriteLine("\nRecords Per Course:");
        foreach (var item in courseCount)
        {
            Console.WriteLine(item.Key + ": " + item.Value);
        }

        LogAction("REPORT", "Generated report");
    }

    // Get next ID
    static int GetNextId()
    {
        string[] lines = File.ReadAllLines(recordFile);

        if (lines.Length == 0)
            return 1;

        int maxId = 0;

        foreach (string line in lines)
        {
            string[] parts = line.Split('|');

            if (parts.Length > 0 && int.TryParse(parts[0], out int id))
            {
                if (id > maxId)
                    maxId = id;
            }
        }

        return maxId + 1;
    }

    // Simple checksum
    static string ComputeChecksum(string name, string course, string concern, string date)
    {
        string data = name + course + concern + date;
        int sum = 0;

        foreach (char c in data)
        {
            sum += c;
        }

        return sum.ToString();
    }

    // Audit log
    static void LogAction(string action, string details)
    {
        string log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {action} | {details}";
        File.AppendAllText(auditFile, log + Environment.NewLine);
    }
}