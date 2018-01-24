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
using System.Globalization;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Device
   {
      /// <summary>[AlphaFS] Gets the physical drive number that is related to the logical drive.</summary>
      /// <returns>The physical drive number that is related to the logical drive, or -1 when the logical drive is a mapped network drive or CDRom.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="Exception"/>
      /// <param name="driveLetter">The logical drive letter, such as C, D.</param>
      [SecurityCritical]
      public static int GetPhysicalDriveNumber(char driveLetter)
      {
         var info = GetPhysicalDriveInfoCore(driveLetter, null);

         return null != info ? info.DeviceNumber : -1;
      }




      /// <summary>Gets the physical drive number that is related to the logical drive.</summary>
      /// <returns>The physical drive number that is related to the logical drive, or -1 when the logical drive is a mapped network drive or CDRom.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="Exception"/>
      /// <param name="driveLetter">The logical drive letter, such as C, D.</param>
      [SecurityCritical]
      internal static NativeMethods.STORAGE_DEVICE_NUMBER? GetPhysicalDriveNumberCore(char driveLetter)
      {
         if (!char.IsLetter(driveLetter))
            throw new ArgumentException(Resources.Argument_must_be_a_drive_letter_from_a_z, "driveLetter");


         // FileSystemRights desiredAccess: If this parameter is zero, the application can query certain metadata such as file, directory, or device attributes
         // without accessing that file or device, even if GENERIC_READ access would have been denied.
         // You cannot request an access mode that conflicts with the sharing mode that is specified by the dwShareMode parameter in an open request that already has an open handle.
         const int dwDesiredAccess = 0;

         // Requires elevation.
         //const FileSystemRights dwDesiredAccess = FileSystemRights.Read | FileSystemRights.Write;

         //const bool elevatedAccess = (dwDesiredAccess & FileSystemRights.Read) != 0 && (dwDesiredAccess & FileSystemRights.Write) != 0;


         var physicalDrive = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", Path.LogicalDrivePrefix, driveLetter.ToString(), Path.VolumeSeparator);


         using (var safeHandle = File.OpenPhysicalDrive(physicalDrive, dwDesiredAccess))

            return GetStorageDeviceDriveNumber(safeHandle, physicalDrive);
      }
   }
}
