using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
    using Sirenix.OdinInspector.Editor;
#endif
using System;
//https://odininspector.com/documentation/sirenix.odininspector.editor.odinattributedrawer-1



[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class BinaryIntAttribute : Attribute
{
    public int binaryBits;
    public bool showInputFiled;
    public BinaryIntAttribute(int Bits = 32,bool showInput = false)
    {
        binaryBits = Bits;
        showInputFiled = showInput;
    }
}

#if UNITY_EDITOR

            
    public sealed class BinaryIntDrawer : OdinAttributeDrawer<BinaryIntAttribute,int>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            int value = this.ValueEntry.SmartValue;
            int largestBit = 0;
            for (int i = 0; i < 32; i++)
            {
                if ((~value & (1 << i)) == 0)
                {
                    largestBit = i;
                }
            }

            int addZeroCount = 0;
            if (largestBit < this.Attribute.binaryBits)
            {
                addZeroCount = this.Attribute.binaryBits - largestBit - 1;
            }

            string addZeroString = "";
            for (int i = 0; i < addZeroCount; i++)
            {
                addZeroString += "0";
            }
            string binary = addZeroString+Convert.ToString(this.ValueEntry.SmartValue, 2);
            EditorGUILayout.BeginHorizontal();
      
            if (Attribute.showInputFiled)
            {
                string labelText = this.ValueEntry.Property.Label.text + "\t" + binary;
                ValueEntry.SmartValue = EditorGUILayout.IntField(labelText, ValueEntry.SmartValue);
            }
            else
            {
                EditorGUILayout.LabelField(this.ValueEntry.Property.Label);
                EditorGUILayout.LabelField(binary);
            }
            EditorGUILayout.EndHorizontal();
            // EditorGUILayout.Slider(label, this.ValueEntry.SmartValue, this.Attribute.Min, this.Attribute.Max);
        }
    }


#endif