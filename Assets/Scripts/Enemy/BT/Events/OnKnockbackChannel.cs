using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/OnKnockbackChannel")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "OnKnockbackChannel", message: "Knockback", category: "Events", id: "d4d80b2cc69a650853d0cab0438210bb")]
public sealed partial class OnKnockbackChannel : EventChannel { }

