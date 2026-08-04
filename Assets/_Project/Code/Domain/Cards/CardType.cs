namespace STSlike.Domain.Cards
{
    /// <summary>
    /// What kind of card this is. Lives in the domain rather than the UI because
    /// rules will eventually key off it ("attacks deal +2", "draw a skill").
    /// </summary>
    public enum CardType
    {
        Attack = 0,
        Skill = 1,
        Power = 2,
    }

    /// <summary>
    /// How rare a card is. Also a rules concept: it drives how often a card can
    /// show up in a reward roll.
    /// </summary>
    public enum CardRarity
    {
        Common = 0,
        Rare = 1,
        Super = 2,
    }
}
