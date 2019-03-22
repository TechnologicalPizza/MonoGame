﻿
namespace Microsoft.Xna.Framework.Graphics
{
    public readonly struct ResourceCreatedEvent
    {
        /// <summary>
        /// The newly created resource object.
        /// </summary>
        public object Resource { get; }

        public ResourceCreatedEvent(object resource)
        {
            Resource = resource;
        }
    }
}
