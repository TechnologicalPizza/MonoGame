﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Lidgren.Network.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources_Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources_Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Lidgren.Network.Properties.Resources.Strings", typeof(Resources_Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bytes in Pool: {0} bytes.
        /// </summary>
        internal static string bytesInPool_X {
            get {
                return ResourceManager.GetString("bytesInPool_X", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Received {0} bytes in {1} messages ({2} fragments) in {3} packets.
        /// </summary>
        internal static string received_X_bytes_X_messages_X_fragments_X_packets {
            get {
                return ResourceManager.GetString("received_X_bytes_X_messages_X_fragments_X_packets", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sent {0} bytes in {1} messages in {2} packets.
        /// </summary>
        internal static string sent_X_bytes_X_messages_X_packets {
            get {
                return ResourceManager.GetString("sent_X_bytes_X_messages_X_packets", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Total Bytes Allocated: {0} bytes.
        /// </summary>
        internal static string totalBytesAllocated_X {
            get {
                return ResourceManager.GetString("totalBytesAllocated_X", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} Connections.
        /// </summary>
        internal static string X_connections {
            get {
                return ResourceManager.GetString("X_connections", resourceCulture);
            }
        }
    }
}
