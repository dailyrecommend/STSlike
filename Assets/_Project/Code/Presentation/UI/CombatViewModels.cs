using System;
using System.Collections.Generic;
using UnityEngine;
using STSlike.Domain.Actors;
using STSlike.Domain.Cards;
using STSlike.Domain.Map;

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

        /// <summary>Damage or block per hit.</summary>
        public readonly int IntentAmount;

        /// <summary>How many times it lands; 1 for a single hit.</summary>
        public readonly int IntentHits;

        /// <summary>Motion authored for the move this enemy is about to make.</summary>
        public readonly string IntentMotion;

        /// <summary>Seconds into that motion at which the first hit lands.</summary>
        public readonly float IntentImpactDelay;

        /// <summary>Gap between the blows of a multi-hit move.</summary>
        public readonly float IntentHitInterval;

        public float HpFraction => MaxHp <= 0 ? 0f : (float)Hp / MaxHp;

        /// <summary>"8" for one hit, "5 x3" for a multi-hit.</summary>
        public string IntentText =>
            IntentHits > 1 ? $"{IntentAmount} x{IntentHits}" : IntentAmount.ToString();

        public EnemyView(string name, int hp, int maxHp, int block,
            string intentKind, int intentAmount, int intentHits,
            string intentMotion = null, float intentImpactDelay = 0f,
            float intentHitInterval = 0f)
        {
            IntentImpactDelay = intentImpactDelay;
            IntentHitInterval = intentHitInterval;
            Name = name;
            Hp = hp;
            MaxHp = maxHp;
            Block = block;
            IntentKind = intentKind;
            IntentAmount = intentAmount;
            IntentHits = intentHits < 1 ? 1 : intentHits;
            IntentMotion = intentMotion;
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
        public readonly CardTarget Target;
        public readonly CardRarity Rarity;

        /// <summary>Motion the character plays for this card.</summary>
        public readonly string AnimationTrigger;

        /// <summary>Seconds into that motion at which the hit should read as landing.</summary>
        public readonly float ImpactDelay;

        /// <summary>Gap between blows when this card hits more than once.</summary>
        public readonly float HitInterval;

        /// <summary>
        /// Whether playing this needs an enemy picked first. Read from the
        /// card's authored target, never from its type — an attack can sweep
        /// the room and a skill can carry a debuff that has to be aimed.
        /// </summary>
        public bool NeedsTarget => Target == CardTarget.SingleEnemy;

        public CardView(int handIndex, string id, int cost, bool isPlayable,
            string name, string description, Sprite artwork, CardType type, CardRarity rarity,
            string animationTrigger = null, float impactDelay = 0f, float hitInterval = 0f,
            CardTarget target = CardTarget.SingleEnemy)
        {
            Target = target;
            AnimationTrigger = animationTrigger;
            ImpactDelay = impactDelay;
            HitInterval = hitInterval;
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

    /// <summary>
    /// One node on the act map. Row and column are the grid slot — the map
    /// screen turns those into positions, so the layout is the UI's decision
    /// while the shape of the act stays the domain's.
    /// </summary>
    public readonly struct MapNodeView
    {
        public readonly int Id;
        public readonly int Row;
        public readonly int Column;
        public readonly MapNodeType Type;

        /// <summary>Can be travelled to right now.</summary>
        public readonly bool IsReachable;

        /// <summary>Where the player currently stands.</summary>
        public readonly bool IsCurrent;

        /// <summary>Already travelled through on this run.</summary>
        public readonly bool IsVisited;

        /// <summary>Ids of the nodes above this one, for drawing the routes.</summary>
        public readonly IReadOnlyList<int> Next;

        public MapNodeView(int id, int row, int column, MapNodeType type,
            bool isReachable, bool isCurrent, bool isVisited, IReadOnlyList<int> next)
        {
            Id = id;
            Row = row;
            Column = column;
            Type = type;
            IsReachable = isReachable;
            IsCurrent = isCurrent;
            IsVisited = isVisited;
            Next = next ?? Array.Empty<int>();
        }
    }

    /// <summary>
    /// Points at one of the figures on stage without handing out a domain
    /// object: the player, or an enemy by its slot in the room.
    /// </summary>
    public readonly struct ActorRef
    {
        public readonly bool IsPlayer;

        /// <summary>Slot in the room; -1 for the player or an unknown actor.</summary>
        public readonly int EnemyIndex;

        public bool IsValid => IsPlayer || EnemyIndex >= 0;

        private ActorRef(bool isPlayer, int enemyIndex)
        {
            IsPlayer = isPlayer;
            EnemyIndex = enemyIndex;
        }

        public static ActorRef Player => new ActorRef(true, -1);
        public static ActorRef Enemy(int index) => new ActorRef(false, index);
        public static ActorRef None => new ActorRef(false, -1);
    }

    /// <summary>
    /// A figure is starting a move: which one, what motion, and how long it
    /// winds up. Raised once per action, so a three-hit flurry plays one
    /// animation rather than three.
    /// </summary>
    public readonly struct ActorActionView
    {
        public readonly ActorRef Actor;
        public readonly string Motion;
        public readonly float ImpactDelay;

        /// <summary>Gap between the blows of a multi-hit move.</summary>
        public readonly float HitInterval;

        public ActorActionView(ActorRef actor, string motion, float impactDelay, float hitInterval)
        {
            Actor = actor;
            Motion = motion;
            ImpactDelay = impactDelay;
            HitInterval = hitInterval;
        }
    }

    /// <summary>
    /// One figure as its own small panel: what a widget floating over its head
    /// needs to draw.
    ///
    /// The player and every enemy are described identically, so a single prefab
    /// serves all of them and a fight with four opponents needs no new UI. The
    /// enemy-only fields simply go unused for the player.
    ///
    /// Status effects will arrive as another field here once the rules have
    /// them; nothing else in the chain has to change to carry them.
    /// </summary>
    public readonly struct ActorHudView
    {
        public readonly ActorRef Actor;
        public readonly string Name;
        public readonly int Hp;
        public readonly int MaxHp;
        public readonly int Block;

        /// <summary>False for the player, whose intent panel stays hidden.</summary>
        public readonly bool IsEnemy;

        /// <summary>What this one will do on its next turn.</summary>
        public readonly IntentType Intent;
        public readonly int IntentAmount;
        public readonly int IntentHits;

        /// <summary>"8" for one hit, "5 x3" for a multi-hit.</summary>
        public string IntentText =>
            IntentHits > 1 ? $"{IntentAmount} x{IntentHits}" : IntentAmount.ToString();

        public ActorHudView(ActorRef actor, string name, int hp, int maxHp, int block,
            bool isEnemy, IntentType intent = IntentType.Unknown,
            int intentAmount = 0, int intentHits = 1)
        {
            Actor = actor;
            Name = name;
            Hp = hp;
            MaxHp = maxHp;
            Block = block;
            IsEnemy = isEnemy;
            Intent = intent;
            IntentAmount = intentAmount;
            IntentHits = intentHits < 1 ? 1 : intentHits;
        }
    }

    /// <summary>Payload for floating damage numbers and hit reactions.</summary>
    public readonly struct HitView
    {
        public readonly ActorRef Source;
        public readonly ActorRef Target;
        public readonly string TargetName;
        public readonly int Amount;

        /// <summary>Convenience for HUD code that only cares about the player.</summary>
        public bool OnPlayer => Target.IsPlayer;

        public HitView(ActorRef source, ActorRef target, string targetName, int amount)
        {
            Source = source;
            Target = target;
            TargetName = targetName;
            Amount = amount;
        }
    }
}
