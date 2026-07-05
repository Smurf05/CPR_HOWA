namespace HOWA.Framework.RFID
{
    public class RfidService
    {
        /// <summary>
        /// Cleanses and validates raw card scanner inputs to check if they match standard RFID UIDs.
        /// </summary>
        public bool ValidateRfidUid(string rawInput, out string cleanedUid)
        {
            cleanedUid = null;
            if (string.IsNullOrWhiteSpace(rawInput))
                return false;

            cleanedUid = rawInput.Trim();

            // Strip out common carriage returns/newlines sent by keyboard-emulating RFID readers
            cleanedUid = cleanedUid.Replace("\r", "").Replace("\n", "");

            if (cleanedUid.Length >= 4 && cleanedUid.Length <= 50)
            {
                return true;
            }

            return false;
        }
    }
}
