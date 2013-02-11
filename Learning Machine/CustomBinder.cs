using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Kinect.Toolbox.Gestures.Learning_Machine
{
    internal class CustomBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            if (typeName == "System.Collections.Generic.List`1[[Microsoft.Xna.Framework.Vector2, Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553]]")
            {
                return typeof(List<Vector2>);
            }
            if (typeName == "Microsoft.Xna.Framework.Vector2")
            {
                return typeof(Vector2);
            }

            return Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
        }
    }
}
