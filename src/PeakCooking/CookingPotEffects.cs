using Newtonsoft.Json;
using Peak.Afflictions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PeakCooking;

// all foods added to the pot are merged into the same mega-handlers
// and then the handlers numbers are scaled down by number of uses
public class CookingPotEffects
{
    public Dictionary<Affliction.AfflictionType, Affliction> Afflictions = new();
    public Dictionary<CharacterAfflictions.STATUSTYPE, float> Statuses = new();

    List<MonoBehaviour> generated = new();

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append("{ ");
        foreach (var type in Statuses.Keys)
        {
            sb.Append($"{type}={Statuses[type]}, ");
        }
        foreach (var type in Afflictions.Keys)
        {
            if (type == Affliction.AfflictionType.AddBonusStamina)
            {
                var action = Afflictions[type] as Affliction_AddBonusStamina;
                if (action != null)
                {
                    sb.Append($"{type}={action.staminaAmount}, ");
                }
            }
            else
            {
                sb.Append($"{type}={Afflictions[type].totalTime}, ");
            }
        }
        sb.Append("}");
        return sb.ToString();
    }

    // copy all types of afflictions
    private static Affliction? CopyAffliction(Affliction obj)
    {
        Affliction? outCopy = null;
        if (obj is Affliction_PoisonOverTime)
        {
            var affliction = obj as Affliction_PoisonOverTime;
            if (affliction != null)
            {
                var copy = new Affliction_PoisonOverTime();
                copy.delayBeforeEffect = affliction.delayBeforeEffect;
                copy.statusPerSecond = affliction.statusPerSecond;
                outCopy = copy;
            }
        }
        if (obj is Affliction_InfiniteStamina)
        {
            var affliction = obj as Affliction_InfiniteStamina;
            if (affliction != null)
            {
                var copy = new Affliction_InfiniteStamina();
                copy.drowsyAffliction = CopyAffliction(affliction.drowsyAffliction);
                copy.climbDelay = affliction.climbDelay;
                outCopy = copy;
            }
        }
        if (obj is Affliction_FasterBoi)
        {
            var affliction = obj as Affliction_FasterBoi;
            if (affliction != null)
            {
                var copy = new Affliction_FasterBoi();
                copy.moveSpeedMod = affliction.moveSpeedMod;
                copy.climbSpeedMod = affliction.climbSpeedMod;
                copy.drowsyOnEnd = affliction.drowsyOnEnd;
                copy.cachedDrowsy = affliction.cachedDrowsy;
                copy.climbDelay = affliction.climbDelay;
                outCopy = copy;
            }
        }
        if (obj is Affliction_Exhaustion)
        {
            var affliction = obj as Affliction_Exhaustion;
            if (affliction != null)
            {
                var copy = new Affliction_Exhaustion();
                copy.drainAmount = affliction.drainAmount;
                outCopy = copy;
            }
        }
        if (obj is Affliction_Glowing)
        {
            var affliction = obj as Affliction_Glowing;
            if (affliction != null)
            {
                // ignore glowing effect
            }
        }
        if (obj is Affliction_AdjustColdOverTime)
        {
            var affliction = obj as Affliction_AdjustColdOverTime;
            if (affliction != null)
            {
                var copy = new Affliction_AdjustColdOverTime();
                copy.statusPerSecond = affliction.statusPerSecond;
                outCopy = copy;
            }
        }
        if (obj is Affliction_Chaos)
        {
            var affliction = obj as Affliction_Chaos;
            if (affliction != null)
            {
                var copy = new Affliction_Chaos();
                copy.statusAmountAverage = affliction.statusAmountAverage;
                copy.statusAmountStandardDeviation = affliction.statusAmountStandardDeviation;
                copy.averageBonusStamina = affliction.averageBonusStamina;
                copy.standardDeviationBonusStamina = affliction.standardDeviationBonusStamina;
                outCopy = copy;
            }
        }
        if (obj is Affliction_AdjustStatus)
        {
            var affliction = obj as Affliction_AdjustStatus;
            if (affliction != null)
            {
                var copy = new Affliction_AdjustStatus();
                copy.statusType = affliction.statusType;
                copy.statusAmount = affliction.statusAmount;
                outCopy = copy;
            }
        }
        if (obj is Affliction_ClearAllStatus)
        {
            var affliction = obj as Affliction_ClearAllStatus;
            if (affliction != null)
            {
                var copy = new Affliction_ClearAllStatus();
                copy.excludeCurse = affliction.excludeCurse;
                outCopy = copy;
            }
        }
        if (obj is Affliction_PreventPoisonHealing)
        {
            var affliction = obj as Affliction_PreventPoisonHealing;
            if (affliction != null)
            {
                var copy = new Affliction_PreventPoisonHealing();
                outCopy = copy;
            }
        }
        if (obj is Affliction_AddBonusStamina)
        {
            var affliction = obj as Affliction_AddBonusStamina;
            if (affliction != null)
            {
                var copy = new Affliction_AddBonusStamina();
                copy.staminaAmount = affliction.staminaAmount;
                outCopy = copy;
            }
        }
        if (obj is Affliction_AdjustDrowsyOverTime)
        {
            var affliction = obj as Affliction_AdjustDrowsyOverTime;
            if (affliction != null)
            {
                var copy = new Affliction_AdjustDrowsyOverTime();
                copy.statusPerSecond = affliction.statusPerSecond;
                outCopy = copy;
            }
        }
        if (obj is Affliction_AdjustStatusOverTime)
        {
            var affliction = obj as Affliction_AdjustStatusOverTime;
            if (affliction != null)
            {
                var copy = new Affliction_AdjustStatusOverTime();
                copy.statusPerSecond = affliction.statusPerSecond;
                outCopy = copy;
            }
        }
        if (outCopy != null)
        {
            outCopy.timeElapsed = obj.timeElapsed;
            outCopy.bonusTime = obj.bonusTime;
            outCopy.totalTime = obj.totalTime;
        }
        return outCopy;
    }

    // Performs output = accumulator + (scale * mult) and writes to toScale
    private static Affliction AddMultiplyAffliction(Affliction? accumulator, float mult, Affliction scale)
    {
        Affliction? output = CopyAffliction(scale);
        if (output == null)
        {
            return scale;
        }
        // don't use accumulator if it isn't valid
        Affliction? acc = accumulator != null &&  output.GetAfflictionType() == accumulator.GetAfflictionType()
            && output.GetType() == accumulator.GetType() ? accumulator : null;

        // cast as needed
        Affliction_InfiniteStamina? output1 = output as Affliction_InfiniteStamina;
        Affliction_InfiniteStamina? acc1 = acc as Affliction_InfiniteStamina;
        Affliction_FasterBoi? output2 = output as Affliction_FasterBoi;
        Affliction_FasterBoi? acc2 = acc as Affliction_FasterBoi;
        Affliction_AddBonusStamina? output3 = output as Affliction_AddBonusStamina;
        Affliction_AddBonusStamina? acc3 = acc as Affliction_AddBonusStamina;

        // multiply and add outputlictions
        if (output1 != null)
        {
            output1.totalTime *= mult;
            if (acc1 != null)
            {
                output1.totalTime += acc1.totalTime;
            }
            if (output1.drowsyAffliction != null)
            {
                output1.drowsyAffliction.totalTime *= mult;
                if (acc1?.drowsyAffliction != null)
                {
                    output1.drowsyAffliction.totalTime += acc1.drowsyAffliction.totalTime;
                }
            }
        }
        else if (output2 != null)
        {
            output2.totalTime *= mult;
            output2.drowsyOnEnd *= mult;
            if (acc2 != null)
            {
                output2.totalTime += acc2.totalTime;
                output2.drowsyOnEnd += acc2.drowsyOnEnd;
            }
        }
        else if (output3 != null)
        {
            output3.totalTime *= mult;
            output3.staminaAmount *= mult;
            if (acc3 != null)
            {
                output3.totalTime += acc3.totalTime;
                output3.staminaAmount += acc3.staminaAmount;
            }
        }
        // default behavior: output time
        else
        {
            output.totalTime *= mult;
            if (acc != null)
            {
                output.totalTime += acc.totalTime;
            }
        }
        return output;
    }

    private void ClearGenerated()
    {
        foreach (var mb in generated)
        {
            UnityEngine.Object.Destroy(mb);
        }
        generated.Clear();
    }

    public void UpdateGenerated(GameObject go)
    {
        ClearGenerated();
        foreach (var type in Statuses.Keys)
        {
            var action = go.AddComponent<Action_ModifyStatus>();
            action.statusType = type;
            action.changeAmount = Statuses[type];
            action.OnCastFinished = true;
            generated.Add(action);
        }
        foreach (var type in Afflictions.Keys)
        {
            var action = go.AddComponent<Action_ApplyAffliction>();
            action.affliction = Afflictions[type];
            action.OnCastFinished = true;
            generated.Add(action);
        }
    }

    public void FromCookingPotItems(List<CookingPot.PotItem> items, int uses)
    {
        Statuses.Clear();
        Afflictions.Clear();
        foreach (CookingPot.PotItem potItem in items)
        {
            ItemDatabase.TryGetItem(potItem.ID, out Item item);
            if (item != null)
            {
                foreach (var action in item.gameObject.GetComponents<Action_RestoreHunger>())
                {
                    if (!action.OnCastFinished && !action.OnConsumed || !action.enabled)
                    {
                        continue;
                    }
                    if (!Statuses.ContainsKey(CharacterAfflictions.STATUSTYPE.Hunger))
                    {
                        Statuses[CharacterAfflictions.STATUSTYPE.Hunger] = 0f;
                    }
                    Statuses[CharacterAfflictions.STATUSTYPE.Hunger] -= action.restorationAmount;
                }
                foreach (var action in item.gameObject.GetComponents<Action_InflictPoison>())
                {
                    if (!action.OnCastFinished && !action.OnConsumed || !action.enabled)
                    {
                        continue;
                    }
                    if (!Statuses.ContainsKey(CharacterAfflictions.STATUSTYPE.Poison))
                    {
                        Statuses[CharacterAfflictions.STATUSTYPE.Poison] = 0f;
                    }
                    Statuses[CharacterAfflictions.STATUSTYPE.Poison] += action.inflictionTime * action.poisonPerSecond;
                }
                foreach (var action in item.gameObject.GetComponents<Action_GiveExtraStamina>())
                {
                    if (!action.OnCastFinished && !action.OnConsumed || !action.enabled)
                    {
                        continue;
                    }
                    var aff = new Affliction_AddBonusStamina();
                    aff.staminaAmount = action.amount;
                    if (!Afflictions.ContainsKey(Affliction.AfflictionType.AddBonusStamina))
                    {
                        Afflictions[Affliction.AfflictionType.AddBonusStamina] = AddMultiplyAffliction(null, potItem.Uses, aff);
                    }
                    else
                    {
                        Afflictions[Affliction.AfflictionType.AddBonusStamina] = AddMultiplyAffliction(
                            Afflictions[Affliction.AfflictionType.AddBonusStamina], potItem.Uses, aff);
                    }
                }
                foreach (var action in item.gameObject.GetComponents<Action_ModifyStatus>())
                {
                    if (!action.OnCastFinished && !action.OnConsumed || !action.enabled)
                    {
                        continue;
                    }
                    if (!Statuses.ContainsKey(action.statusType))
                    {
                        Statuses[action.statusType] = 0f;
                    }
                    Statuses[action.statusType] += action.changeAmount * potItem.Uses;
                }
                foreach (var action in item.gameObject.GetComponents<Action_ApplyAffliction>())
                {
                    if (!action.OnCastFinished && !action.OnConsumed || !action.enabled)
                    {
                        continue;
                    }
                    foreach (Affliction affliction in new List<Affliction>() { action.affliction }.Concat(action.extraAfflictions.ToArray()))
                    {
                        Affliction.AfflictionType type = affliction.GetAfflictionType();
                        Affliction? aff = CopyAffliction(affliction);
                        if (aff != null)
                        {
                            if (!Afflictions.ContainsKey(type))
                            {
                                Afflictions[type] = AddMultiplyAffliction(null, potItem.Uses, aff);
                            }
                            else
                            {
                                Afflictions[type] = AddMultiplyAffliction(Afflictions[type], potItem.Uses, aff);
                            }
                        }
                    }
                }
            }
            else
            {
                throw new PeakCookingException($"ID {potItem.ID} is not valid.");
            }
        }
        // scale down by uses
        foreach (var type in new List<CharacterAfflictions.STATUSTYPE>(Statuses.Keys))
        {
            Statuses[type] = Statuses[type] / uses;
        }
        foreach (var type in new List<Affliction.AfflictionType>(Afflictions.Keys))
        {
            Afflictions[type] = AddMultiplyAffliction(null, 1 / (float)uses, Afflictions[type]);
        }
    }
}
