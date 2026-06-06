namespace FlightBooking.Entities
{
    public class CheckInExtra
    {
        public string CheckInExtraId { get; set; }
        public string CheckInId { get; set; }
        public string ExtraType { get; set; } // Baggage, Food, SeatUpgrade
        public string ExtraName { get; set; }
        public decimal Price { get; set; }
    }
}
