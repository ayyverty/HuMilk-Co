using Verse;
using RJWQuirksFork.Condition;

namespace HuMilkCo.Condition
{
    /// <summary>
    /// satisfiedBy condition for the 'Lactation' quirk: true when the sex partner
    /// is currently lactating. The fork grants the ThatsMyFetish thought on the
    /// quirk-holder when satisfied during sex.
    /// </summary>
    public class PartnerIsLactating : ConditionBase
    {
        public override bool SatisfiedByPartner(Pawn pawn, Pawn partner)
        {
            return MilkHelper.IsLactating(partner);
        }
    }
}
