using UnityEngine;
using STSlike.Domain.Cards;

namespace STSlike.Presentation.UI
{
    /// <summary>
    /// Flat, display-ready snapshots handed to the UI.
    ///
    /// The UI never receives domain objects. It cannot reach into a Deck or a
    /// CombatManager, so the rules can be refactored without breaking a single
    /// prefab, and nothing on screen can accidentally mutate game state.
    /// </summary>
    public readonly struct RunView
    {
        public readonly string CharacterId;

        /// <summary>Which encounter of the act; the "floor" shown in the top bar.</summary>
        public readonly int Floor;
        public readonly int TotalFloors;

        public readonly int Credits;

        /// <summary>Cards owned for the whole run (not the cards in hand).</summary>
        public readonly int DeckCount;

        public readonly int Seed;
        public readonly bool IsFinished;
        public readonly bool PlayerWon;

        public RunView(string characterId, int floor, int totalFloors, int credits,
            int deckCount, int seed, bool isFinished, bool playerWon)
        {
            CharacterId = characterId;
            Floor = floor;
            TotalFloors = totalFloors;
            Credits = credits;
            DeckCount = deckCount;
            Seed = seed;
            IsFinished = isFinished;
            PlayerWon = playerWon;
        }
    }

    public readonly struct PlayerView
    {
        public readonly string Name;
        public readonly int Hp;
        public readonly int MaxHp;
        public readonly int Block;
        public readonly int Energy;
        public readonly int MaxEnergy;

        /// <summary>Pile counts for the draw/discard widgets at the bottom of the screen.</summary>
        public readonly int DrawCount;
        public readonly int DiscardCount;
        public readonly int HandCount;

        /// <summary>0..1, ready for a bar fill.</summary>
        public float HpFraction => MaxHp <= 0 ? 0f : (float)Hp / MaxHp;

        public PlayerView(string name, int hp, int maxHp, int block, int energy, int maxEnergy,
            int drawCount, int discardCount, int handCount)
        {
            Name = name;
            Hp = hp;
            MaxHp = maxHp;
            Block = block;
            Energy = energy;
            MaxEnergy = maxEnergy;
            DrawCount = drawCount;
            DiscardCount = discardCount;
            HandCount = handCount;
        }
    }

    public readonly struct EnemyView
    {
        public readonly string Name;
        public readonly int Hp;
        public readonly int MaxHp;
        public readonly int Block;
        public readonly string IntentKind;
        public readonly int IntentAmount;

        public float HpFraction => MaxHp <= 0 ? 0f : (float)Hp / MaxHp;

        public EnemyView(string name, int hp, int maxHp, int block, string intentKind, int intentAmount)
        {
            Name = name;
            Hp = hp;
            MaxHp = maxHp;
            Block = block;
            IntentKind = intentKind;
            IntentAmount = intentAmount;
        }
    }

    /// <summary>
    /// One card in hand. <see cref="HandIndex"/> is how the UI plays it — the UI
    /// never holds a reference to the card object itself.
    /// </summary>
    public readonly struct CardView
    {
        public readonly int HandIndex;
        public readonly string Id;
        public readonly int Cost;
        public readonly bool IsPlayable;

        /// <summary>Display name, already falling back to the id if unauthored.</summary>
        public readonly string Name;

        /// <summary>Description with the effect numbers already filled in.</summary>
        public readonly string Description;

        public readonly Sprite Artwork;
        public readonly CardType Type;
        public readonly CardRarity Rarity;

        public CardView(int handIndex, string id, int cost, bool isPlayable,
            string name, string description, Sprite artwork, CardType type, CardRarity rarity)
        {
            HandIndex = handIndex;
            Id = id;
            Cost = cost;
            IsPlayable = isPlayable;
            Name = name;
            Description = description;
            Artwork = artwork;
            Type = type;
            Rarity = rarity;
        }
    }

    public enum RewardKind
    {
        AddCard,
        Heal,
        Other,
    }

    public readonly struct RewardView
    {
        public readonly int OptionIndex;
        public readonly RewardKind Kind;

        /// <summary>Set for AddCard rewards.</summary>
        public readonly string CardId;
        public readonly int CardCost;

        /// <summary>Set for Heal rewards.</summary>
        public readonly int Amount;

        public RewardView(int optionIndex, RewardKind kind, string cardId, int cardCost, int amount)
        {
            OptionIndex = optionIndex;
            Kind = kind;
            CardId = cardId;
            CardCost = cardCost;
            Amount = amount;
        }
    }

    /// <summary>Payload for floating damage numbers and hit reactions.</summary>
    public readonly struct HitView
    {
        public readonly bool OnPlayer;
        public readonly string TargetName;
        public readonly int Amount;

        public HitView(bool onPlayer, string targetName, int amount)
        {
            OnPlayer = onPlayer;
            TargetName = targetName;
            Amount = amount;
        }
    }
}
