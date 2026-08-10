using System;
using System.Collections;
using UnityEngine;

namespace STSlike.Presentation.View
{
    /// <summary>
    /// One figure on stage: the model, its animator, and the point UI should
    /// hover over.
    ///
    /// It is told the *name* of a motion — "Attack", "Defend", or whatever a
    /// card asks for — and looks it up. Because the name comes from data, an
    /// artist can give every card and every enemy move its own motion without a
    /// single code change; a name with no matching trigger is simply skipped, so
    /// missing animations never break a fight.
    /// </summary>
    public sealed class ActorView : MonoBehaviour
    {
        /// <summary>Renames a requested motion to whatever this rig actually calls it.</summary>
        [Serializable]
        private struct AnimationAlias
        {
            [Tooltip("What the game asks for, e.g. the card's trigger name.")]
            public string requested;

            [Tooltip("The Animator trigger on this rig.")]
            public string trigger;
        }

        /// <summary>
        /// How a particular clip is paced. Belongs to the animation rather than
        /// to whatever asked for it: the same swing takes the same time whoever
        /// called for it.
        /// </summary>
        [Serializable]
        private struct MotionTiming
        {
            [Tooltip("The motion as it actually plays, stance prefix included.")]
            public string motion;

            [Tooltip("Seconds from the start of the clip until the blow lands. " +
                     "-1 keeps what the card asked for.")]
            public float impactDelay;

            [Tooltip("Seconds between blows of a multi-hit clip. " +
                     "-1 keeps what the card asked for.")]
            public float hitInterval;
        }

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Tooltip("The resting motion. Played again once an action finishes.")]
        [SerializeField] private string idleMotion = "Idle";

        [Tooltip("Return to idle automatically when a motion ends. Turn this off " +
                 "if the Animator already has exit transitions back to idle.")]
        [SerializeField] private bool autoReturnToIdle = true;

        [Tooltip("Used when the clip length cannot be read.")]
        [SerializeField, Min(0f)] private float idleFallbackDelay = 1f;

        [Tooltip("Seconds of blend when a motion starts. 0 snaps to the first " +
                 "frame — which is what makes a repeated motion read as a fresh " +
                 "one. Raise it if a change of pose is too abrupt, at the cost " +
                 "of two quick motions bleeding into each other. The flinch " +
                 "always snaps regardless.")]
        [SerializeField, Min(0f)] private float motionBlend;

        [Tooltip("Played when this figure is hit. Leave empty to skip.")]
        [SerializeField] private string hitTrigger = "Hit";

        [Header("Stances")]
        [Tooltip("While a stance is held, every motion is looked up with the " +
                 "stance's name in front of it first, then falls back.\n\n" +
                 "The name is whatever was ASKED FOR — a card whose trigger is " +
                 "\"Bang\" wants \"Focus_Bang\", not \"Focus_Shot\". Idle becomes " +
                 "\"Focus_Idle\".\n\n" +
                 "Only make the ones that actually differ; the rest fall back " +
                 "silently.")]
        [SerializeField] private string stanceSeparator = "_";

        [Tooltip("Optional motions for the change itself — \"Focus_Enter\" and " +
                 "\"Focus_Exit\". Leave them off the rig if the stance cards " +
                 "carry their own animation, which is the usual arrangement.")]
        [SerializeField] private string enterSuffix = "Enter";
        [SerializeField] private string exitSuffix = "Exit";

        [Header("Motion Timing")]
        [Tooltip("When a blow lands within a particular clip, for clips whose " +
                 "timing differs from the card's own. Listed by the motion that " +
                 "actually plays — \"Focus_Bang\", not \"Bang\" — so a stance " +
                 "version can be paced differently from the plain one.\n\n" +
                 "A motion with no entry keeps whatever the card or the enemy's " +
                 "routine asked for, which is right for most of them.")]
        [SerializeField] private MotionTiming[] motionTimings;

        [Tooltip("Played when this figure falls. Leave empty if the rig has none.")]
        [SerializeField] private string deathTrigger = "";

        [Tooltip("Optional renames, for rigs that use different trigger names.")]
        [SerializeField] private AnimationAlias[] aliases;

        [Tooltip("Warn in the console when a requested motion does not exist on " +
                 "this rig. Useful while animations are still being made.")]
        [SerializeField] private bool logMissingMotions = true;

        [Header("Anchors")]
        [Tooltip("Where a health bar or damage number should appear. " +
                 "Defaults to this object.")]
        [SerializeField] private Transform uiAnchor;

        [Header("Death")]
        [Tooltip("Seconds before the figure is hidden. Gives a death animation " +
                 "time to play; set 0 to hide at once.")]
        [SerializeField] private float hideAfterDeath = 1.2f;

        [Tooltip("Leave the figure standing when it dies. Useful for the player, " +
                 "who has no death animation yet.")]
        [SerializeField] private bool hideOnDeath = true;

        private bool _dead;
        private Coroutine _idleWatch;
        private string _stance;

        /// <summary>Point in world space for UI pinned to this figure.</summary>
        public Transform UiAnchor => uiAnchor != null ? uiAnchor : transform;

        public bool IsDead => _dead;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        /// <summary>Plays a named motion, or does nothing if this rig has none.</summary>
        public void Play(string motion)
        {
            if (_dead)
                return;

            Restart(InStance(motion), motionBlend);
            ScheduleIdle();
        }

        /// <summary>
        /// The stance this figure is holding, or null. Changing it plays the way
        /// in or the way out, and from then on every motion prefers its stance
        /// version. Setting the same stance twice does nothing.
        /// </summary>
        public void SetStance(string stance)
        {
            if (_dead || _stance == stance)
                return;

            string leaving = _stance;
            _stance = stance;

            // Transition motions are optional. A card that changes stance
            // usually carries its own animation, and the stance breaking under
            // a blow is covered by the flinch — so these are only played by a
            // rig that actually has them, rather than warned about.
            PlayIfPresent(leaving, exitSuffix);
            PlayIfPresent(stance, enterSuffix);

            ScheduleIdle();
        }

        private void PlayIfPresent(string stance, string suffix)
        {
            if (string.IsNullOrEmpty(stance))
                return;

            string motion = Resolve(Join(stance, suffix));
            if (HasMotion(motion))
                Restart(motion, motionBlend);
        }

        /// <summary>
        /// Prefers the stance's version of a motion, falling back to the plain
        /// one. So a rig only needs the motions that actually differ while
        /// standing that way.
        /// </summary>
        private string InStance(string motion) => MotionIn(_stance, motion);

        private string MotionIn(string stance, string motion)
        {
            string plain = Resolve(motion);
            if (string.IsNullOrEmpty(stance) || string.IsNullOrEmpty(plain))
                return plain;

            string variant = Resolve(Join(stance, motion));
            return HasMotion(variant) ? variant : plain;
        }

        private string Join(string stance, string motion) => stance + stanceSeparator + motion;

        /// <summary>
        /// Adjusts a card's pacing to whatever this rig's version of the motion
        /// actually needs.
        ///
        /// Asked with what the card requested and the stance that will be in
        /// effect when it plays — not the one on screen now. A change of stance
        /// waits its turn in the queue, so entering a stance and firing in the
        /// same breath would otherwise be paced as though the figure were still
        /// standing the old way.
        ///
        /// Values left at -1 in the table, or a motion not in it at all, keep
        /// what came in.
        /// </summary>
        public void ApplyTiming(string motion, string stance,
            ref float impactDelay, ref float hitInterval)
        {
            if (motionTimings == null || string.IsNullOrEmpty(motion))
                return;

            string playing = MotionIn(stance, motion);
            if (string.IsNullOrEmpty(playing))
                return;

            foreach (MotionTiming timing in motionTimings)
            {
                if (timing.motion != playing)
                    continue;

                if (timing.impactDelay >= 0f)
                    impactDelay = timing.impactDelay;
                if (timing.hitInterval >= 0f)
                    hitInterval = timing.hitInterval;

                return;
            }
        }

        /// <summary>
        /// Plays the flinch. Always snaps, whatever the blend is set to: blows
        /// can land closer together than any blend is long, and a flurry has to
        /// read as separate hits.
        /// </summary>
        public void PlayHit()
        {
            if (_dead)
                return;

            // Whatever posture was being held, the blow has broken it — the
            // rules drop the stance on the same hit, so the figure comes out of
            // it here rather than flinching in a pose it no longer has.
            _stance = null;

            Restart(Resolve(hitTrigger), 0f);
            ScheduleIdle();
        }

        /// <summary>Returns to the resting motion straight away.</summary>
        public void PlayIdle()
        {
            StopIdleWatch();
            if (!_dead)
                Restart(InStance(idleMotion), motionBlend);
        }

        public void PlayDeath()
        {
            if (_dead)
                return;

            StopIdleWatch();   // a corpse must not stand back up
            _dead = true;
            Restart(Resolve(deathTrigger), motionBlend);

            if (!hideOnDeath)
                return;

            if (hideAfterDeath > 0f)
                Invoke(nameof(HideCorpse), hideAfterDeath);
            else
                HideCorpse();
        }

        /// <summary>Puts a figure back to a clean state before it is reused.</summary>
        public void ResetActor()
        {
            CancelInvoke(nameof(HideCorpse));
            StopIdleWatch();
            _dead = false;
            _stance = null;
            gameObject.SetActive(true);

            if (animator != null)
                animator.Rebind();
        }

        /// <summary>
        /// Goes back to idle once the motion has run its course.
        ///
        /// A rig whose clips are played as states has no exit transitions, so it
        /// would otherwise stand frozen on the last frame of its attack. The
        /// clip's own length is used, read a frame later because the Animator
        /// has not entered the new state yet at the moment Play is called.
        /// </summary>
        private void ScheduleIdle()
        {
            if (!autoReturnToIdle || animator == null || string.IsNullOrEmpty(idleMotion))
                return;

            StopIdleWatch();
            _idleWatch = StartCoroutine(ReturnToIdle());
        }

        private void StopIdleWatch()
        {
            if (_idleWatch != null)
            {
                StopCoroutine(_idleWatch);
                _idleWatch = null;
            }
        }

        private IEnumerator ReturnToIdle()
        {
            yield return null;   // let the Animator enter the new state

            float wait = idleFallbackDelay;
            if (animator != null)
            {
                // Mid-blend the current state is still the one being left, so
                // the motion just asked for is the *next* one.
                AnimatorStateInfo state = animator.IsInTransition(0)
                    ? animator.GetNextAnimatorStateInfo(0)
                    : animator.GetCurrentAnimatorStateInfo(0);

                if (!state.loop && state.length > 0f)
                    wait = state.length;
            }

            yield return new WaitForSeconds(wait);

            _idleWatch = null;
            if (!_dead)
                Restart(InStance(idleMotion), motionBlend);
        }

        private string Resolve(string motion)
        {
            if (string.IsNullOrEmpty(motion) || aliases == null)
                return motion;

            foreach (AnimationAlias alias in aliases)
            {
                if (alias.requested == motion)
                    return alias.trigger;
            }
            return motion;
        }

        private void HideCorpse() => gameObject.SetActive(false);

        /// <summary>
        /// Plays a motion by name, from the beginning, interrupting itself if it
        /// is already running.
        ///
        /// The state is tried before a trigger of the same name, which is the
        /// opposite of what an Animator is usually driven with. A trigger is
        /// consumed into a transition, so asking for a motion that is already
        /// running does nothing at all — the second of two quick attacks, or the
        /// second blow of a flurry, would simply be swallowed. Only a state can
        /// be rewound. Rigs built by dropping clips in have no triggers anyway.
        /// </summary>
        private void Restart(string motion, float blend)
        {
            if (animator == null || string.IsNullOrEmpty(motion))
                return;

            int hash = Animator.StringToHash(motion);
            for (int layer = 0; layer < animator.layerCount; layer++)
            {
                if (!animator.HasState(layer, hash))
                    continue;

                // The explicit zero is what rewinds it: without a start time,
                // playing the state already running is ignored.
                if (blend > 0f)
                    animator.CrossFadeInFixedTime(hash, blend, layer, 0f);
                else
                    animator.Play(hash, layer, 0f);
                return;
            }

            // No state to jump to, so a trigger is all that is left. It cannot
            // rewind, but clearing it first at least stops requests that arrived
            // faster than the Animator could consume them from queueing up and
            // playing late, after the action they belonged to is over.
            if (HasTrigger(motion))
            {
                animator.ResetTrigger(motion);
                animator.SetTrigger(motion);
                return;
            }

            if (logMissingMotions)
            {
                Debug.LogWarning(
                    $"[ActorView] '{name}' has no trigger or state called '{motion}'. " +
                    "Add it to the Animator, or map it under Aliases.", this);
            }
        }

        /// <summary>
        /// Whether this rig can play that motion at all. Used to fall back from
        /// a stance's version to the plain one without warning about the version
        /// that was never made — most motions do not need a stance variant.
        /// </summary>
        private bool HasMotion(string motion)
        {
            if (animator == null || string.IsNullOrEmpty(motion))
                return false;

            int hash = Animator.StringToHash(motion);
            for (int layer = 0; layer < animator.layerCount; layer++)
            {
                if (animator.HasState(layer, hash))
                    return true;
            }

            return HasTrigger(motion);
        }

        private bool HasTrigger(string motion)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == motion)
                    return true;
            }
            return false;
        }
    }
}
