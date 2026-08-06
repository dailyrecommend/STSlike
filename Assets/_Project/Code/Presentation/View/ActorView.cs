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

            Restart(Resolve(motion), motionBlend);
            ScheduleIdle();
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

            Restart(Resolve(hitTrigger), 0f);
            ScheduleIdle();
        }

        /// <summary>Returns to the resting motion straight away.</summary>
        public void PlayIdle()
        {
            StopIdleWatch();
            if (!_dead)
                Restart(Resolve(idleMotion), motionBlend);
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
                Restart(Resolve(idleMotion), motionBlend);
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
