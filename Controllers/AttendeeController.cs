using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace AttendanceTrackerServer.Controllers
{
    [ApiController]
    public class AttendeeController : ControllerBase
    {
        private readonly ILogger<AttendeeController> _logger;
        private static string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TECAttendeeTracker";
        private static string file = dir + "\\AttendeeStatus.json";
        private string MasterAuth = Environment.GetEnvironmentVariable("TECMasterAuth") ?? "NULL!";

        public AttendeeController(ILogger<AttendeeController> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetAttendee")]
        public IEnumerable<Attendee> GetAttendee(int? ID)
        {
            Console.WriteLine("Get Attendee Request");

            // get a list of attendees if no ID provided
            List<Attendee>? attendees = GetAttendees();
            Console.WriteLine($"Got Attendees: {JsonConvert.SerializeObject(attendees)}");
            if (attendees == null) { return null; }
            Console.WriteLine("Got Attendees");

            // or a specific attendee if ID provided
            if (ID != null) { attendees = [attendees.Find((attendee) => { return attendee.ID == ID; })]; }
            return attendees;
        }

        [HttpPost("AttendeeJoin")]
        public string AttendeeJoin(int? ID, [FromForm]string Auth)
        {
            Console.WriteLine("Attendee Join Request");

            if (Auth != MasterAuth)
            {
                return "AuthError";
            }

            // error if no ID provided
            if (ID == null)
            {
                return "NOID";
            }

            // or set attendee status to joined, and add join date to list
            List<Attendee>? attendees = GetAttendees();
            if (attendees == null) { return "NOATTENDEES"; }

            (Attendee attendee, int index) = GetAttendeeAndIndex(attendees, ID);
            if (attendee == null) { return "NOTFOUND"; }

            attendee.AtCon = true;
            attendee.JoinDates.Add(DateTime.Now);
            attendees[index] = attendee;

            WriteFile(attendees);

            return "SUCCESS";
        }

        [HttpPost("AttendeeLeft")]
        public string AttendeeLeft(int? ID, [FromForm] string Auth)
        {
            Console.WriteLine("Attendee Left Request");

            if (Auth != MasterAuth)
            {
                return "AuthError";
            }

            // error if no ID provided
            if (ID == null)
            {
                return "NOID";
            }

            // or set attendee status to left, and add leave date to list
            List<Attendee>? attendees = GetAttendees();
            if (attendees == null) { return "NOATTENDEES"; }

            (Attendee attendee, int index) = GetAttendeeAndIndex(attendees, ID); 
            if (attendee == null) { return "NOTFOUND"; }

            attendee.AtCon = false;
            attendee.LeaveDates.Add(DateTime.Now);
            attendees[index] = attendee;

            WriteFile(attendees);

            return "SUCCESS";
        }

        [HttpPost("RegisterAttendee")]
        public string RegisterAttendee(string Name, bool AtCon, [FromForm] string Auth)
        {
            Console.WriteLine("Register Attendee Request");

            if (Auth != MasterAuth)
            {
                return "AuthError";
            }

            // error if any info missing
            if (Name == null)
            {
                return "NONAME";
            }

            List<Attendee>? attendees = GetAttendees();

            // create a new attendee
            Attendee attendee = new(Name, attendees == null ? 0 : attendees.Last().ID + 1, AtCon, [], []);

            (attendees ??= []).Add(attendee);

            WriteFile(attendees);

            return "SUCCESS";
        }

        List<Attendee>? GetAttendees()
        {
            outQueue.Enqueue((null, true, HttpContext.TraceIdentifier));
            List<Attendee>? attendees = null;
            // loop checking the inQueue until we find the result from the request we sent in
            while (true)
            {
                if (inQueue.Any((item) => 
                    { 
                        if (item.Item2 == HttpContext.TraceIdentifier) 
                        { 
                            inQueue.Remove(item); 
                            attendees = item.Item1; 
                            return true; 
                        } 
                        return false;
                    }))
                {
                    return attendees;
                }
            }
        }

        (Attendee, int) GetAttendeeAndIndex(List<Attendee>? attendees, int? ID)
        {
            Attendee? attendee = attendees.Find((attendee) => { return attendee.ID == ID; });
            int index = attendees.IndexOf(attendee);
            return (attendee, index);
        }

        void WriteFile(List<Attendee> attendees) => outQueue.Enqueue((attendees, false, "")); 

        static Queue<(List<Attendee>?, bool, string)> outQueue = new Queue<(List<Attendee>?, bool, string)>();
        static List<(List<Attendee>?, string)> inQueue = new List<(List<Attendee>?, string)>();

        public static void FileAccess()
        {
            // keep checking queue for file accesses
            while (true)
            {
                while (!outQueue.Any()) { /* Camellia - SPIN ETERNALLY starts playing */ }

                //when queue has one available, check whether read or write, and perform it
                (List<Attendee>?, bool, string) obj = outQueue.Dequeue();
                List<Attendee>? list = obj.Item1;
                bool readWrite = obj.Item2;
                string requestID = obj.Item3;

                if (list == null && readWrite == true && requestID != "")
                {
                    // read operation
                    try
                    {
                        string fileCont;
                        using (StreamReader streamReader = new StreamReader(file, Encoding.UTF8))
                        {
                            fileCont = streamReader.ReadToEnd();
                        }
                        List<Attendee>? attendees = JsonConvert.DeserializeObject<List<Attendee>>(fileCont);
                        // somehow return the list to the function that enqueued it?
                        inQueue.Add((attendees, requestID));
                    }
                    catch { }
                }
                else if (list != null && readWrite == false && requestID == "")
                {
                    // write operation
                    string toWrite = JsonConvert.SerializeObject(list, Formatting.Indented);
                    if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); };
                    StreamWriter sw = new StreamWriter(file);
                    sw.Write(toWrite);
                    sw.Close();
                }
                else { Console.WriteLine($"File Queue Error! List: {list != null} ReadWrite: {readWrite} RequestID: {requestID}"); }
            }
        }
    }
}
