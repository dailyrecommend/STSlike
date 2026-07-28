using System;
using STSlike.Domain.Effects;

namespace STSlike.Domain.Commands
{
    /// <summary>
    /// A command that runs one contextualized card effect (which effect, with
    /// which source/target). This is where Strategy (the effect) and Command
    /// (the queued execution) meet.
    /// </summary>
    public sealed class EffectCommand : ICommand
    {
        private readonly ICardEffect _effect;
        private readonly EffectContext _context;

        public EffectCommand(ICardEffect effect, EffectContext context)
        {
            _effect = effect ?? throw new ArgumentNullException(nameof(effect));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Execute() => _effect.Execute(_context);
    }
}
