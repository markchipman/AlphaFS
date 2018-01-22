/*  Copyright (C) 2008-2017 Peter Palotas, Jeffrey Jangli, Alexandr Normuradov
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy 
 *  of this software and associated documentation files (the "Software"), to deal 
 *  in the Software without restriction, including without limitation the rights 
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 *  copies of the Software, and to permit persons to whom the Software is 
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 *  THE SOFTWARE. 
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>[AlphaFS] Provides static methods to retrieve device resource information from a local or remote host.</summary>
   public static partial class Device
   {
      /// <summary>Builds a Device Interface Detail Data structure.</summary>
      /// <returns>An initialized NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA instance.</returns>
      [SecurityCritical]
      private static NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA GetInterfaceDetails(SafeHandle safeHandle, ref NativeMethods.SP_DEVICE_INTERFACE_DATA interfaceData, ref NativeMethods.SP_DEVINFO_DATA infoData)
      {
         var didd = new NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA
         {
            cbSize = (uint) (IntPtr.Size == 4 ? 6 : 8)
         };


         var success = NativeMethods.SetupDiGetDeviceInterfaceDetail(safeHandle, ref interfaceData, ref didd, (uint) Marshal.SizeOf(didd), IntPtr.Zero, ref infoData);

         var lastError = Marshal.GetLastWin32Error();
         if (!success)
            NativeError.ThrowException(lastError);


         return didd;
      }
      

      [SecurityCritical]
      private static string GetDeviceRegistryProperty(SafeHandle safeHandle, NativeMethods.SP_DEVINFO_DATA infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum property)
      {
         using (var safeBuffer = new SafeGlobalMemoryBufferHandle(NativeMethods.DefaultFileBufferSize / 4))
         {
            uint regDataType;

            var success = NativeMethods.SetupDiGetDeviceRegistryProperty(safeHandle, ref infoData, property, out regDataType, safeBuffer, (uint) safeBuffer.Capacity, IntPtr.Zero);

            var lastError = Marshal.GetLastWin32Error();


            // MSDN: SetupDiGetDeviceRegistryProperty returns the ERROR_INVALID_DATA error code if the requested property
            // does not exist for a device or if the property data is not valid.

            if (!success)
            {
               if (lastError != Win32Errors.ERROR_INVALID_DATA)
                  NativeError.ThrowException(lastError);

               return string.Empty;
            }


            return safeBuffer.PtrToStringUni();
         }
      }
      
      
      [SecurityCritical]
      internal static T GetDeviceIoData<T>(SafeFileHandle safeHandle, string path)
      {
         using (var safeBuffer = GetDeviceIoData<T>(safeHandle, NativeMethods.IoControlCode.IOCTL_STORAGE_GET_DEVICE_NUMBER, path))

            return safeBuffer.PtrToStructure<T>(0);
      }


      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Object needs to be disposed by caller.")]
      [SecurityCritical]
      internal static SafeGlobalMemoryBufferHandle GetDeviceIoData<T>(SafeFileHandle safeHandle, NativeMethods.IoControlCode controlCode, string path, int size = -1)
      {
         NativeMethods.IsValidHandle(safeHandle);


         var bufferSize = size > -1 ? size : Marshal.SizeOf(typeof(T));

         while (true)
         {
            uint bytesReturned;

            var safeBuffer = new SafeGlobalMemoryBufferHandle(bufferSize);

            var success = NativeMethods.DeviceIoControl(safeHandle, controlCode, IntPtr.Zero, 0, safeBuffer, (uint) safeBuffer.Capacity, out bytesReturned, IntPtr.Zero);

            var lastError = Marshal.GetLastWin32Error();

            if (success)
               return safeBuffer;


            bufferSize = GetDoubledBufferSizeOrThrowException(lastError, safeBuffer, bufferSize, path);
         }
      }


      [SecurityCritical]
      private static int GetDoubledBufferSizeOrThrowException(int lastError, SafeHandle safeBuffer, int bufferSize, string path)
      {
         if (null != safeBuffer && !safeBuffer.IsClosed)
            safeBuffer.Close();


         switch ((uint)lastError)
         {
            case Win32Errors.ERROR_MORE_DATA:
            case Win32Errors.ERROR_INSUFFICIENT_BUFFER:
               bufferSize = 2 * bufferSize;
               break;


            default:
               NativeMethods.IsValidHandle(safeBuffer, lastError, string.Format(CultureInfo.InvariantCulture, "Buffer size: {0}. Path: {1}", bufferSize.ToString(CultureInfo.InvariantCulture), path));
               break;
         }


         return bufferSize;
      }


      /// <summary>Invokes InvokeIoControl with the specified input and specified size.</summary>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Object needs to be disposed by caller.")]
      [SecurityCritical]
      internal static SafeGlobalMemoryBufferHandle InvokeDeviceIoData<T>(SafeFileHandle safeHandle, NativeMethods.IoControlCode controlCode, T anyObject, string path, int size = -1)
      {
         NativeMethods.IsValidHandle(safeHandle);


         var bufferSize = size > -1 ? size : Marshal.SizeOf(anyObject);

         while (true)
         {
            uint bytesReturned;

            var safeBuffer = new SafeGlobalMemoryBufferHandle(bufferSize);

            var success = NativeMethods.DeviceIoControlAnyObject(safeHandle, controlCode, anyObject, (uint) bufferSize, safeBuffer, (uint) safeBuffer.Capacity, out bytesReturned, IntPtr.Zero);

            var lastError = Marshal.GetLastWin32Error();


            if (success)
               return safeBuffer;


            bufferSize = GetDoubledBufferSizeOrThrowException(lastError, safeBuffer, bufferSize, path);
         }
      }
      

      [SecurityCritical]
      private static void SetDeviceProperties(SafeHandle safeHandle, DeviceInfo deviceInfo, NativeMethods.SP_DEVINFO_DATA infoData)
      {
         deviceInfo.BaseContainerId = new Guid(GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.BaseContainerId));

         deviceInfo.CompatibleIds = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.CompatibleIds);

         deviceInfo.DeviceClass = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.Class);
         
         deviceInfo.DeviceDescription = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.DeviceDescription);

         //deviceInfo.DeviceType = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.DeviceType);

         deviceInfo.Driver = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.Driver);

         deviceInfo.EnumeratorName = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.EnumeratorName);

         deviceInfo.FriendlyName = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.FriendlyName);

         deviceInfo.HardwareId = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.HardwareId);

         deviceInfo.LocationInformation = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.LocationInformation);

         deviceInfo.LocationPaths = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.LocationPaths);

         deviceInfo.Manufacturer = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.Manufacturer);

         deviceInfo.PhysicalDeviceObjectName = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.PhysicalDeviceObjectName);

         deviceInfo.Service = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.Service);
      }


      [SecurityCritical]
      private static void SetMinimalDeviceProperties(SafeHandle safeHandle, DeviceInfo deviceInfo, NativeMethods.SP_DEVINFO_DATA infoData)
      {
         deviceInfo.DeviceClass  = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.Class);

         //deviceInfo.DeviceType = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.DeviceType);

         deviceInfo.FriendlyName = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.FriendlyName);
      }


      //private static uint IOCTL_STORAGE_QUERY_PROPERTY = CTL_CODE((uint) NativeMethods.STORAGE_DEVICE_TYPE.FILE_DEVICE_MASS_STORAGE, 0x500, 0,0);
      //private static uint CTL_CODE(uint deviceType, uint function, uint method, uint access)
      //{
      //   return (deviceType << 16) | (access << 14) | (function << 2) | method;
      //}
   }
}
