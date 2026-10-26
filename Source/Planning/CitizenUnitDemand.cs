namespace AutomatedPublicTransportPlanning.Planning
{
    /// <summary>Converts capacity-based citizen units into people for the preview model.</summary>
    internal static class CitizenUnitDemand
    {
        internal const int PeoplePerUnit = 5;

        public static bool Accumulate(CitizenUnit.Flags flags, ref int homePeople,
                                      ref int workPeople, ref int visitPeople)
        {
            bool hasDemand = false;
            if ((flags & CitizenUnit.Flags.Home) != CitizenUnit.Flags.None)
            {
                homePeople += PeoplePerUnit;
                hasDemand = true;
            }
            if ((flags & CitizenUnit.Flags.Work) != CitizenUnit.Flags.None)
            {
                workPeople += PeoplePerUnit;
                hasDemand = true;
            }
            if ((flags & CitizenUnit.Flags.Visit) != CitizenUnit.Flags.None)
            {
                visitPeople += PeoplePerUnit;
                hasDemand = true;
            }

            return hasDemand;
        }
    }
}
