﻿using UnityEngine;

namespace AltFunding
{
    static class Utilities
    {
        public static T GetComponentInChild<T>(this GameObject go, string name) where T : Component
        {
            var gameObject = go.GetChild(name);
            if(gameObject == null)
            {
                Debug.Log("No GameObject named " + name + " found in the children of " + go.name);
                return null;
            }
            var component = gameObject.GetComponent<T>();

            if(component == null)
            {
                Debug.Log("No Component of type " + typeof(T).Name + " found in GameObject " + name);
                return null;
            }
            return component;
        }
    }
}
