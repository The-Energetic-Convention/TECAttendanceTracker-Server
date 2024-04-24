namespace AttendanceTrackerServer
{
    public class Attendee
    {
        public Attendee(string name, int iD, bool atCon, List<DateTime> joinDates, List<DateTime> leaveDates)
        {
            Name = name;
            ID = iD;
            AtCon = atCon;
            JoinDates = joinDates;
            LeaveDates = leaveDates;
        }

        public string Name { get; set; }
        public int ID { get; set; }
        public bool AtCon {  get; set; }
        public List<DateTime> JoinDates { get; set; }
        public List<DateTime> LeaveDates { get; set; }
    }
}
