using Peak.Afflictions;
using Photon.Pun;
using System;
using System.Collections.Generic;

namespace PeakCooking;

public class Action_CookingPotConsume : ItemAction
{
    public override void RunAction()
    {
        var itemComponent = item.GetComponent<CookingPot>();
        if (itemComponent != null)
        {
            itemComponent.RemoveRandomItem();
        }
    }
}
