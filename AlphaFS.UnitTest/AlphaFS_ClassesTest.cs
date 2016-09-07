/*  Copyright (C) 2008-2015 Peter Palotas, Jeffrey Jangli, Alexandr Normuradov
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

using Alphaleonis.Win32.Filesystem;
using Alphaleonis.Win32.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using DriveInfo = Alphaleonis.Win32.Filesystem.DriveInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using OperatingSystem = Alphaleonis.Win32.OperatingSystem;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace AlphaFS.UnitTest
{
   /// <summary>This is a test class for several AlphaFS instance classes.</summary>
   [TestClass]
   public partial class AlphaFS_ClassesTest
   {
      #region Unit Tests

      #region DumpClassByHandleFileInfo

      private void DumpClassByHandleFileInfo(bool isLocal)
      {
         Console.WriteLine("\n=== TEST {0} ===", isLocal ? UnitTestConstants.Local : UnitTestConstants.Network);
         string tempPath = Path.GetTempPath("File.GetFileInfoByHandle()-" + Path.GetRandomFileName());
         if (!isLocal) tempPath = Path.LocalToUnc(tempPath);

         Console.WriteLine("\nInput File Path: [{0}]", tempPath);

         FileStream stream = File.Create(tempPath);
         stream.WriteByte(1);

         UnitTestConstants.StopWatcher(true);
         ByHandleFileInfo bhfi = File.GetFileInfoByHandle(stream.SafeFileHandle);
         Console.WriteLine(UnitTestConstants.Reporter());

         Assert.IsTrue(UnitTestConstants.Dump(bhfi, -18));

         Assert.AreEqual(System.IO.File.GetCreationTimeUtc(tempPath), bhfi.CreationTimeUtc);
         Assert.AreEqual(System.IO.File.GetLastAccessTimeUtc(tempPath), bhfi.LastAccessTimeUtc);
         Assert.AreEqual(System.IO.File.GetLastWriteTimeUtc(tempPath), bhfi.LastWriteTimeUtc);

         stream.Close();

         File.Delete(tempPath, true);
         Assert.IsFalse(File.Exists(tempPath), "Cleanup failed: File should have been removed.");
         Console.WriteLine();
      }

      #endregion // DumpClassByHandleFileInfo

      #region DumpClassDeviceInfo

      private void DumpClassDeviceInfo(string host)
      {
         Console.WriteLine("\n=== TEST ===");

         bool allOk = false;
         try
         {
            #region DeviceGuid.Volume

            Console.Write("\nEnumerating volumes from host: [{0}]\n", host);
            int cnt = 0;
            UnitTestConstants.StopWatcher(true);
            foreach (DeviceInfo device in Device.EnumerateDevices(host, DeviceGuid.Volume))
            {
               Console.WriteLine("\n#{0:000}\tClass: [{1}]", ++cnt, device.Class);

               UnitTestConstants.Dump(device, -24);

               try
               {
                  string getDriveLetter = Volume.GetDriveNameForNtDeviceName(device.PhysicalDeviceObjectName);
                  string dosdeviceGuid = Volume.GetVolumeGuidForNtDeviceName(device.PhysicalDeviceObjectName);

                  Console.WriteLine("\n\tVolume.GetDriveNameForNtDeviceName() : [{0}]", getDriveLetter ?? "null");
                  Console.WriteLine("\tVolume.GetVolumeGuidForNtDeviceName(): [{0}]", dosdeviceGuid ?? "null");
               }
               catch (Exception ex)
               {
                  Console.WriteLine("\nCaught (unexpected) {0}: [{1}]", ex.GetType().FullName, ex.Message.Replace(Environment.NewLine, "  "));
               }
            }
            Console.WriteLine(UnitTestConstants.Reporter());
            Console.WriteLine();

            #endregion // DeviceGuid.Volume

            #region DeviceGuid.Disk

            Console.Write("\nEnumerating disks from host: [{0}]\n", host);
            cnt = 0;
            UnitTestConstants.StopWatcher(true);
            foreach (DeviceInfo device in Device.EnumerateDevices(host, DeviceGuid.Disk))
            {
               Console.WriteLine("\n#{0:000}\tClass: [{1}]", ++cnt, device.Class);

               UnitTestConstants.Dump(device, -24);

               try
               {
                  string getDriveLetter = Volume.GetDriveNameForNtDeviceName(device.PhysicalDeviceObjectName);
                  string dosdeviceGuid = Volume.GetVolumeGuidForNtDeviceName(device.PhysicalDeviceObjectName);

                  Console.WriteLine("\n\tVolume.GetDriveNameForNtDeviceName() : [{0}]", getDriveLetter ?? "null");
                  Console.WriteLine("\tVolume.GetVolumeGuidForNtDeviceName(): [{0}]", dosdeviceGuid ?? "null");
               }
               catch (Exception ex)
               {
                  Console.WriteLine("\nCaught (unexpected) {0}: [{1}]", ex.GetType().FullName, ex.Message.Replace(Environment.NewLine, "  "));
               }
            }
            Console.WriteLine(UnitTestConstants.Reporter());

            #endregion // DeviceGuid.Disk

            allOk = true;
         }
         catch (Exception ex)
         {
            Console.WriteLine("\tCaught (unexpected) {0}: [{1}]", ex.GetType().FullName, ex.Message.Replace(Environment.NewLine, "  "));
         }

         Assert.IsTrue(allOk, "Could (probably) not connect to host: [{0}]", host);
         Console.WriteLine();
      }

      #endregion // DumpClassDeviceInfo

      #region DumpClassDiskSpaceInfo

      private void DumpClassDiskSpaceInfo(bool isLocal)
      {
         Console.WriteLine("\n=== TEST {0} ===", isLocal ? UnitTestConstants.Local : UnitTestConstants.Network);
         string tempPath = UnitTestConstants.SysDrive;
         if (!isLocal) tempPath = Path.LocalToUnc(tempPath);
         Console.WriteLine("\nInput Path: [{0}]", tempPath);

         int cnt = 0;
         int errorCount = 0;

          // Get only .IsReady drives.
         foreach (var drv in Directory.EnumerateLogicalDrives(false, true))
         {
            if (drv.DriveType == DriveType.NoRootDirectory)
               continue;

            string drive = isLocal ? drv.Name : Path.LocalToUnc(drv.Name);

            UnitTestConstants.StopWatcher(true);

            try
            {
               // null (default) == All information.
               DiskSpaceInfo dsi = drv.DiskSpaceInfo;
               string report = UnitTestConstants.Reporter(true);

               Console.WriteLine("\n#{0:000}\tInput Path: [{1}]{2}", ++cnt, drive, report);
               Assert.IsTrue(UnitTestConstants.Dump(dsi, -26));

               Assert.AreNotEqual((int) 0, (int) dsi.BytesPerSector);
               Assert.AreNotEqual((int) 0, (int) dsi.SectorsPerCluster);
               Assert.AreNotEqual((int) 0, (int) dsi.TotalNumberOfClusters);
               Assert.AreNotEqual((int) 0, (int) dsi.TotalNumberOfBytes);

               if (drv.DriveType == DriveType.CDRom)
               {
                  Assert.AreEqual((int) 0, (int) dsi.FreeBytesAvailable);
                  Assert.AreEqual((int) 0, (int) dsi.NumberOfFreeClusters);
                  Assert.AreEqual((int) 0, (int) dsi.TotalNumberOfFreeBytes);
               }
               else
               {
                  Assert.AreNotEqual((int) 0, (int) dsi.FreeBytesAvailable);
                  Assert.AreNotEqual((int) 0, (int) dsi.NumberOfFreeClusters);
                  Assert.AreNotEqual((int) 0, (int) dsi.TotalNumberOfFreeBytes);
               }

               // false == Size information only.
               dsi = Volume.GetDiskFreeSpace(drive, false);
               Assert.AreEqual((int)0, (int)dsi.BytesPerSector);
               Assert.AreEqual((int)0, (int)dsi.NumberOfFreeClusters);
               Assert.AreEqual((int)0, (int)dsi.SectorsPerCluster);
               Assert.AreEqual((int)0, (int)dsi.TotalNumberOfClusters);


               // true == Cluster information only.
               dsi = Volume.GetDiskFreeSpace(drive, true);
               Assert.AreEqual((int)0, (int)dsi.FreeBytesAvailable);
               Assert.AreEqual((int)0, (int)dsi.TotalNumberOfBytes);
               Assert.AreEqual((int)0, (int)dsi.TotalNumberOfFreeBytes);
            }
            catch (Exception ex)
            {
               Console.WriteLine("\n\nCaught (unexpected) {0}: [{1}]", ex.GetType().FullName, ex.Message.Replace(Environment.NewLine, "  "));
               errorCount++;
            }
         }

         if (cnt == 0)
            Assert.Inconclusive("Nothing was enumerated.");

         Assert.AreEqual(0, errorCount, "No errors were expected.");
         Console.WriteLine();
      }

      #endregion // DumpClassDiskSpaceInfo

      #region DumpClassDriveInfo

      private void DumpClassDriveInfo(bool isLocal, string drive)
      {
         Console.WriteLine("\n=== TEST {0} ===", isLocal ? UnitTestConstants.Local : UnitTestConstants.Network);
         string tempPath = drive;
         if (!isLocal) tempPath = Path.LocalToUnc(tempPath);

         UnitTestConstants.StopWatcher(true);
         DriveInfo actual = new DriveInfo(tempPath);
         Console.WriteLine("\nInput Path: [{0}]{1}", tempPath, UnitTestConstants.Reporter());

         #region UnitTestConstants.Local Drive
         if (isLocal)
         {
            // System.IO.DriveInfo() can not handle UNC paths.
            System.IO.DriveInfo expected = new System.IO.DriveInfo(tempPath);
            //Dump(expected, -21);

            // Even 1 byte more or less results in failure, so do these tests asap.
            Assert.AreEqual(expected.AvailableFreeSpace, actual.AvailableFreeSpace, "AvailableFreeSpace AlphaFS != System.IO");
            Assert.AreEqual(expected.TotalFreeSpace, actual.TotalFreeSpace, "TotalFreeSpace AlphaFS != System.IO");
            Assert.AreEqual(expected.TotalSize, actual.TotalSize, "TotalSize AlphaFS != System.IO");

            Assert.AreEqual(expected.DriveFormat, actual.DriveFormat, "DriveFormat AlphaFS != System.IO");
            Assert.AreEqual(expected.DriveType, actual.DriveType, "DriveType AlphaFS != System.IO");
            Assert.AreEqual(expected.IsReady, actual.IsReady, "IsReady AlphaFS != System.IO");
            Assert.AreEqual(expected.Name, actual.Name, "Name AlphaFS != System.IO");
            Assert.AreEqual(expected.RootDirectory.ToString(), actual.RootDirectory.ToString(), "RootDirectory AlphaFS != System.IO");
            Assert.AreEqual(expected.VolumeLabel, actual.VolumeLabel, "VolumeLabel AlphaFS != System.IO");
         }

         #region Class Equality

         ////int getHashCode1 = actual.GetHashCode();

         //DriveInfo driveInfo2 = new DriveInfo(tempPath);
         ////int getHashCode2 = driveInfo2.GetHashCode();

         //DriveInfo clone = actual;
         ////int getHashCode3 = clone.GetHashCode();

         //bool isTrue1 = clone.Equals(actual);
         //bool isTrue2 = clone == actual;
         //bool isTrue3 = !(clone != actual);
         //bool isTrue4 = actual == driveInfo2;
         //bool isTrue5 = !(actual != driveInfo2);

         ////Console.WriteLine("\n\t\t actual.GetHashCode() : [{0}]", getHashCode1);
         ////Console.WriteLine("\t\t     clone.GetHashCode() : [{0}]", getHashCode3);
         ////Console.WriteLine("\t\tdriveInfo2.GetHashCode() : [{0}]\n", getHashCode2);

         ////Console.WriteLine("\t\t obj clone.ToString() == [{0}]", clone.ToString());
         ////Console.WriteLine("\t\t obj clone.Equals()   == [{0}] : {1}", TextTrue, isTrue1);
         ////Console.WriteLine("\t\t obj clone ==         == [{0}] : {1}", TextTrue, isTrue2);
         ////Console.WriteLine("\t\t obj clone !=         == [{0}]: {1}\n", TextFalse, isTrue3);

         ////Console.WriteLine("\t\tdriveInfo == driveInfo2 == [{0}] : {1}", TextTrue, isTrue4);
         ////Console.WriteLine("\t\tdriveInfo != driveInfo2 == [{0}] : {1}", TextFalse, isTrue5);

         //Assert.IsTrue(isTrue1, "clone.Equals(actual)");
         //Assert.IsTrue(isTrue2, "clone == actual");
         //Assert.IsTrue(isTrue3, "!(clone != actual)");
         //Assert.IsTrue(isTrue4, "actual == driveInfo2");
         //Assert.IsTrue(isTrue5, "!(actual != driveInfo2)");

         #endregion // Class Equality

         #endregion // UnitTestConstants.Local Drive

         UnitTestConstants.StopWatcher(true);
         UnitTestConstants.Dump(actual, -21);

         UnitTestConstants.Dump(actual.DiskSpaceInfo, -26);

         //if (expected != null) Dump(expected.RootDirectory, -17);
         //Dump(actual.RootDirectory, -17);

         UnitTestConstants.Dump(actual.VolumeInfo, -26);

         Console.WriteLine(UnitTestConstants.Reporter());
         Console.WriteLine();
      }

      #endregion // DumpClassDriveInfo
     
      #region DumpClassFileSystemEntryInfo

      private void DumpClassFileSystemEntryInfo(bool isLocal)
      {
         Console.WriteLine("\n=== TEST {0} ===", isLocal ? UnitTestConstants.Local : UnitTestConstants.Network);

         Console.WriteLine("\nThe return type is based on C# inference. Possible return types are:");
         Console.WriteLine("string (full path), FileSystemInfo (DiskInfo / FileInfo) or FileSystemEntryInfo instance.\n");

         #region Directory

         string path = UnitTestConstants.SysRoot;
         if (!isLocal) path = Path.LocalToUnc(path);

         Console.WriteLine("\nInput Directory Path: [{0}]", path);

         Console.WriteLine("\n\nvar fsei = Directory.GetFileSystemEntry(path);");
         var asFileSystemEntryInfo = File.GetFileSystemEntryInfo(path);
         Assert.IsTrue((asFileSystemEntryInfo.GetType().IsEquivalentTo(typeof(FileSystemEntryInfo))));
         Assert.IsTrue(UnitTestConstants.Dump(asFileSystemEntryInfo, -17));

         Console.WriteLine();

         #endregion // Directory

         #region File

         path = UnitTestConstants.NotepadExe;
         if (!isLocal) path = Path.LocalToUnc(path);

         Console.WriteLine("\nInput File Path: [{0}]", path);

         Console.WriteLine("\n\nvar fsei = File.GetFileSystemEntry(path);");
         asFileSystemEntryInfo = File.GetFileSystemEntryInfo(path);
         Assert.IsTrue((asFileSystemEntryInfo.GetType().IsEquivalentTo(typeof(FileSystemEntryInfo))));
         Assert.IsTrue(UnitTestConstants.Dump(asFileSystemEntryInfo, -17));

         Console.WriteLine();

         #endregion // File
      }

      #endregion // DumpClassFileSystemEntryInfo

      #region DumpClassShell32Info

      private void DumpClassShell32Info(bool isLocal)
      {
         Console.WriteLine("\n=== TEST {0} ===", isLocal ? UnitTestConstants.Local : UnitTestConstants.Network);
         string tempPath = Path.GetTempPath("Class.Shell32Info()-" + Path.GetRandomFileName());
         if (!isLocal) tempPath = Path.LocalToUnc(tempPath);

         Console.WriteLine("\nInput File Path: [{0}]\n", tempPath);

         using (File.Create(tempPath)) {}


         
         UnitTestConstants.StopWatcher(true);
         Shell32Info shell32Info = Shell32.GetShell32Info(tempPath);
         string report = UnitTestConstants.Reporter();

         Console.WriteLine("\tMethod: Shell32Info.Refresh()");
         Console.WriteLine("\tMethod: Shell32Info.GetIcon()");
         

         string cmd = "print";
         Console.WriteLine("\tMethod: Shell32Info.GetVerbCommand(\"{0}\") == [{1}]", cmd, shell32Info.GetVerbCommand(cmd));

         Assert.IsTrue(UnitTestConstants.Dump(shell32Info, -15));

         File.Delete(tempPath, true);
         Assert.IsFalse(File.Exists(tempPath), "Cleanup failed: File should have been removed.");

         Console.WriteLine("\n{0}", report);
         Console.WriteLine();
      }

      #endregion // DumpClassShell32Info

      #region DumpClassVolumeInfo

      private void DumpClassVolumeInfo(bool isLocal)
      {
         Console.WriteLine("\n=== TEST {0} ===", isLocal ? UnitTestConstants.Local : UnitTestConstants.Network);
         Console.WriteLine("\nEnumerating logical drives.");

         int cnt = 0;
         foreach (string drive in Directory.GetLogicalDrives())
         {
            string tempPath = drive;
            if (!isLocal) tempPath = Path.LocalToUnc(tempPath);

            UnitTestConstants.StopWatcher(true);
            try
            {
               VolumeInfo volInfo = Volume.GetVolumeInfo(tempPath);
               Console.WriteLine("\n#{0:000}\tLogical Drive: [{1}]", ++cnt, tempPath);
               UnitTestConstants.Dump(volInfo, -26);
            }
            catch (Exception ex)
            {
               Console.WriteLine("#{0:000}\tLogical Drive: [{1}]\n\tCaught: {2}: {3}", ++cnt, tempPath, ex.GetType().FullName, ex.Message.Replace(Environment.NewLine, "  "));
            }
            Console.WriteLine();
         }

         if (cnt == 0)
            Assert.Inconclusive("Nothing was enumerated.");

         Console.WriteLine();
      }

      #endregion // DumpClassVolumeInfo



      #region DumpClassDfsInfo

      private void DumpClassDfsInfo()
      {
         int cnt = 0;
         bool noDomainConnection = true;

         UnitTestConstants.StopWatcher(true);
         try
         {
            foreach (string dfsNamespace in Host.EnumerateDomainDfsRoot())
            {
               noDomainConnection = false;

               try
               {
                  Console.Write("\n#{0:000}\tDFS Root: [{1}]\n", ++cnt, dfsNamespace);

                  DfsInfo dfsInfo = Host.GetDfsInfo(dfsNamespace);
                  
                  UnitTestConstants.Dump(dfsInfo, -21);


                  Console.Write("\n\tNumber of Storages: [{0}]\n", dfsInfo.StorageInfoCollection.Count());

                  foreach (DfsStorageInfo store in dfsInfo.StorageInfoCollection)
                     UnitTestConstants.Dump(store, -19);

                  Console.WriteLine();
               }
               catch (NetworkInformationException ex)
               {
                  Console.WriteLine("\n\tNetworkInformationException #1: [{0}]", ex.Message.Replace(Environment.NewLine, "  "));
               }
               catch (Exception ex)
               {
                  Console.WriteLine("\n\t(1) {0}: [{1}]", ex.GetType().FullName, ex.Message.Replace(Environment.NewLine, "  "));
               }
            }
            Console.Write("\n{0}", UnitTestConstants.Reporter());

            if (cnt == 0)
               Assert.Inconclusive("Nothing was enumerated.");
         }
         catch (NetworkInformationException ex)
         {
            Console.WriteLine("\n\tNetworkInformationException #2: [{0}]", ex.Message.Replace(Environment.NewLine, "  "));
         }
         catch (Exception ex)
         {
            Console.WriteLine("\n\t(2) {0}: [{1}]", ex.GetType().FullName, ex.Message.Replace(Environment.NewLine, "  "));
         }

         Console.WriteLine("\n\n\t{0}", UnitTestConstants.Reporter(true));

         if (noDomainConnection)
            Assert.Inconclusive("Test ignored because the computer is probably not connected to a domain.");
         
         if (cnt == 0)
            Assert.Inconclusive("Nothing was enumerated.");

         Console.WriteLine();
      }

      #endregion // DumpClassDfsInfo

      #region DumpOpenConnectionInfo

      private void DumpOpenConnectionInfo(string host)
      {
         Console.WriteLine("\n=== TEST ===");
         Console.WriteLine("\nNetwork.Host.EnumerateOpenResources() from host: [{0}]", host);
          
         UnitTestConstants.StopWatcher(true);
         foreach (OpenConnectionInfo connectionInfo in Host.EnumerateOpenConnections(host, "IPC$", false))
            UnitTestConstants.Dump(connectionInfo, -16);

         Console.WriteLine(UnitTestConstants.Reporter(true));
         Console.WriteLine();
      }
      
      #endregion // DumpClassOpenResourceInfo

      #region DumpClassOpenResourceInfo

      private void DumpClassOpenResourceInfo(string host, string share)
      {
         Console.WriteLine("\n=== TEST ===");
         string tempPath = Path.LocalToUnc(share);
         Console.WriteLine("\nNetwork.Host.EnumerateOpenResources() from host: [{0}]", tempPath);

         Directory.SetCurrentDirectory(tempPath);

         UnitTestConstants.StopWatcher(true);
         int cnt = 0;
         foreach (OpenResourceInfo openResource in Host.EnumerateOpenResources(host, null, null, false))
         {
            if (UnitTestConstants.Dump(openResource, -11))
            {
               Console.Write("\n");
               cnt++;
            }
         }

         Console.WriteLine(UnitTestConstants.Reporter());
         
         if (cnt == 0)
            Assert.Inconclusive("Nothing was enumerated.");

         Console.WriteLine();
      }
      
      #endregion // DumpClassOpenResourceInfo

      #region DumpClassShareInfo

      #endregion // DumpClassShareInfo

      #endregion // Unit Tests

      #region Unit Test Callers

      #region Filesystem

      #region Filesystem_Class_ByHandleFileInfo

      [TestMethod]
      public void Filesystem_Class_ByHandleFileInfo()
      {
         Console.WriteLine("Class Filesystem.ByHandleFileInfo()");

         DumpClassByHandleFileInfo(true);
         DumpClassByHandleFileInfo(false);
      }

      #endregion // Filesystem_Class_ByHandleFileInfo

      #region Filesystem_Class_DeviceInfo

      [TestMethod]
      public void Filesystem_Class_DeviceInfo()
      {
         Console.WriteLine("Class Filesystem.DeviceInfo()");
         Console.WriteLine("\nMSDN Note: Beginning in Windows 8 and Windows Server 2012 functionality to access remote machines has been removed.");
         Console.WriteLine("You cannot access remote machines when running on these versions of Windows.\n");

         DumpClassDeviceInfo(UnitTestConstants.LocalHost);
      }

      #endregion // Filesystem_Class_Shell32Info

      #region Filesystem_Class_DiskSpaceInfo

      [TestMethod]
      public void Filesystem_Class_DiskSpaceInfo()
      {
         Console.WriteLine("Class Filesystem.DiskSpaceInfo()");

         DumpClassDiskSpaceInfo(true);
         DumpClassDiskSpaceInfo(false);
      }

      #endregion // Filesystem_Class_DiskSpaceInfo

      #region Filesystem_Class_DriveInfo

      [TestMethod]
      public void Filesystem_Class_DriveInfo()
      {
         Console.WriteLine("Class Filesystem.DriveInfo()");

         DumpClassDriveInfo(true, UnitTestConstants.SysDrive);
         DumpClassDriveInfo(false, UnitTestConstants.SysDrive);
      }

      #endregion // Filesystem_Class_DriveInfo
     
      #region Filesystem_Class_FileSystemEntryInfo

      [TestMethod]
      public void Filesystem_Class_FileSystemEntryInfo()
      {
         Console.WriteLine("Class Filesystem.FileSystemEntryInfo()");

         DumpClassFileSystemEntryInfo(true);
         DumpClassFileSystemEntryInfo(false);
      }

      #endregion // Filesystem_Class_FileSystemEntryInfo

      #region Filesystem_Class_Shell32Info

      [TestMethod]
      public void Filesystem_Class_Shell32Info()
      {
         Console.WriteLine("Class Filesystem.Shell32Info()");

         DumpClassShell32Info(true);
         DumpClassShell32Info(false);
      }

      #endregion // Filesystem_Class_Shell32Info

      #region Filesystem_Class_VolumeInfo

      [TestMethod]
      public void Filesystem_Class_VolumeInfo()
      {
         Console.WriteLine("Class Filesystem.VolumeInfo()");

         DumpClassVolumeInfo(true);
         DumpClassVolumeInfo(false);
      }

      #endregion // Filesystem_Class_VolumeInfo

      #endregion Filesystem

      #region Network

      #region Network_Class_DfsXxx

      [TestMethod]
      public void Network_Class_DfsXxx()
      {
         Console.WriteLine("Class Network.DfsInfo()");
         Console.WriteLine("Class Network.DfsStorageInfo()");

         DumpClassDfsInfo();
      }

      #endregion // Network_Class_DfsXxx

      #region Network_Class_OpenConnectionInfo

      [TestMethod]
      public void Network_Class_OpenConnectionInfo()
      {
         Console.WriteLine("Class Network.OpenConnectionInfo()");

         if (!UnitTestConstants.IsAdmin())
             Assert.Inconclusive();

         DumpOpenConnectionInfo(UnitTestConstants.LocalHost);
      }

      #endregion // Network_Class_OpenConnectionInfo

      #region Network_Class_OpenResourceInfo

      [TestMethod]
      public void Network_Class_OpenResourceInfo()
      {
         Console.WriteLine("Class Network.OpenResourceInfo()");

         if (!UnitTestConstants.IsAdmin())
             Assert.Inconclusive();

         DumpClassOpenResourceInfo(UnitTestConstants.LocalHost, UnitTestConstants.LocalHostShare);
      }

      #endregion // Network_Class_OpenResourceInfo

      #region Network_Class_ShareInfo

      [TestMethod]
      public void Network_Class_ShareInfo()
      {
         Console.WriteLine("Class Network.ShareInfo()");

         string host = UnitTestConstants.LocalHost;

         Console.WriteLine("\n=== TEST ===");
         Console.Write("\nNetwork.Host.EnumerateShares() from host: [{0}]\n", host);

         int cnt = 0;
         UnitTestConstants.StopWatcher(true);
         foreach (ShareInfo share in Host.EnumerateShares(host, true))
         {
            Console.WriteLine("\n\t#{0:000}\tShare: [{1}]", ++cnt, share);
            UnitTestConstants.Dump(share, -18);
         }

         Console.WriteLine("\n{0}", UnitTestConstants.Reporter(true));

         if (cnt == 0)
            Assert.Inconclusive("Nothing was enumerated.");
      }

      #endregion // Network_Class_ShareInfo

      #endregion // Network

      #region OperatingSystem

      #region Class_OperatingSystem

      [TestMethod]
      public void Class_OperatingSystem()
      {
         Console.WriteLine("Class Win32.OperatingSystem()\n");

         UnitTestConstants.StopWatcher(true);

         Console.WriteLine("VersionName          : [{0}]", OperatingSystem.VersionName);
         Console.WriteLine("OsVersion            : [{0}]", OperatingSystem.OSVersion);
         Console.WriteLine("ServicePackVersion   : [{0}]", OperatingSystem.ServicePackVersion);
         Console.WriteLine("IsServer             : [{0}]", OperatingSystem.IsServer);
         Console.WriteLine("IsWow64Process       : [{0}]", OperatingSystem.IsWow64Process);
         Console.WriteLine("ProcessorArchitecture: [{0}]", OperatingSystem.ProcessorArchitecture);

         Console.WriteLine("\nOperatingSystem.IsAtLeast()\n");

         Console.WriteLine("\tOS Earlier           : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.Earlier));
         Console.WriteLine("\tWindows 2000         : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.Windows2000));
         Console.WriteLine("\tWindows XP           : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.WindowsXP));
         Console.WriteLine("\tWindows Vista        : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.WindowsVista));
         Console.WriteLine("\tWindows 7            : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.Windows7));
         Console.WriteLine("\tWindows 8            : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.Windows8));
         Console.WriteLine("\tWindows 8.1          : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.Windows81));
         Console.WriteLine("\tWindows 10           : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.Windows10));

         Console.WriteLine("\tWindows Server 2003  : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.WindowsServer2003));
         Console.WriteLine("\tWindows Server 2008  : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.WindowsServer2008));
         Console.WriteLine("\tWindows Server 2008R2: [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.WindowsServer2008R2));
         Console.WriteLine("\tWindows Server 2012  : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.WindowsServer2012));
         Console.WriteLine("\tWindows Server 2012R2: [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.WindowsServer2012R2));
         Console.WriteLine("\tWindows Server       : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.WindowsServer));

         Console.WriteLine("\tOS Later             : [{0}]", OperatingSystem.IsAtLeast(OperatingSystem.EnumOsName.Later));

         Console.WriteLine();
         Console.WriteLine(UnitTestConstants.Reporter());
      }

      #endregion // Class_OperatingSystem

      #endregion // OperatingSystem

      #endregion Unit Test Callers
   }
}
