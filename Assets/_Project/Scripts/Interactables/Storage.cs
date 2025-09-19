using Sporae.Core;
using UnityEngine;

namespace _Project
{
    public abstract class Storage : MonoBehaviour
    {
        public abstract Inventory GetInventory();
    }
}