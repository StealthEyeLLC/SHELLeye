using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace SHELLeye;

public static partial class WindowsNative
{
    private const uint GENERIC_WRITE=0x40000000, FILE_READ_ATTRIBUTES=0x80, DELETE_ACCESS=0x00010000;
    private const uint CREATE_ALWAYS=2, FILE_FLAG_BACKUP_SEMANTICS=0x02000000;
    private const uint FILE_BEGIN=0;
    private const int FileBasicInfo=0, FileRenameInfo=3, FileDispositionInfo=4, FileIdInfo=18, FileDispositionInfoEx=21;
    private const uint FILE_DISPOSITION_DELETE=0x00000001, FILE_DISPOSITION_POSIX_SEMANTICS=0x00000002;
    public const uint FSCTL_READ_FILE_USN_DATA=0x000900eb, FSCTL_QUERY_USN_JOURNAL=0x000900f4;
    [StructLayout(LayoutKind.Sequential)] private struct FILE_BASIC_INFO { public long CreationTime,LastAccessTime,LastWriteTime,ChangeTime; public uint FileAttributes; }

    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool GetFileInformationByHandleEx(IntPtr h,int cls,IntPtr info,uint size);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool SetFileInformationByHandle(IntPtr h,int cls,IntPtr info,uint size);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool GetFileSizeEx(IntPtr h,out long size);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool SetFilePointerEx(IntPtr h,long distance,out long newPos,uint method);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool SetEndOfFile(IntPtr h);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool WriteFile(IntPtr h,byte[] buffer,uint count,out uint written,IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool ReadFile(IntPtr h,byte[] buffer,uint count,out uint read,IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] private static extern bool DeviceIoControl(IntPtr h,uint code,IntPtr inBuffer,uint inSize,IntPtr outBuffer,uint outSize,out uint bytes,IntPtr overlapped);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern bool CreateHardLinkW(string newName,string existing,IntPtr attrs);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern bool ReplaceFileW(string replaced,string replacement,string? backup,uint flags,IntPtr exclude,IntPtr reserved);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern bool GetVolumeNameForVolumeMountPointW(string mount,StringBuilder name,uint len);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern bool GetVolumeInformationW(string root,StringBuilder? volumeName,uint volumeNameSize,out uint serial,out uint maxComponent,out uint flags,StringBuilder fs,uint fsSize);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern bool GetDiskFreeSpaceExW(string dir,out ulong freeAvailable,out ulong total,out ulong totalFree);

    public static IntPtr OpenPath(string path,bool write=false,bool create=false)
    {
        var sa=new SECURITY_ATTRIBUTES{nLength=Marshal.SizeOf<SECURITY_ATTRIBUTES>(),bInheritHandle=false};
        uint access=GENERIC_READ|FILE_READ_ATTRIBUTES|(write?(GENERIC_WRITE|DELETE_ACCESS):0);
        IntPtr h=CreateFileW(path,access,FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE,ref sa,create?CREATE_ALWAYS:OPEN_EXISTING,FILE_ATTRIBUTE_NORMAL|FILE_FLAG_BACKUP_SEMANTICS,IntPtr.Zero);
        if(h==new IntPtr(-1))
        {
            int e=Marshal.GetLastWin32Error();
            if(e==2||e==3)throw new ShellEyeException("not_found","File/directory does not exist.",e);
            if(e==5)throw new ShellEyeException("access_denied","Windows denied file access.",e);
            if(e==32||e==33)throw new ShellEyeException("sharing_violation","File sharing state prevents the operation.",e);
            throw new ShellEyeException("native_error","CreateFile failed: "+new Win32Exception(e).Message,e);
        }
        return h;
    }

    public static FileIdentity QueryFileIdentity(IntPtr h)
    {
        IntPtr p=Marshal.AllocHGlobal(24);
        try
        {
            if(!GetFileInformationByHandleEx(h,FileIdInfo,p,24))throw Win32("native_error","GetFileInformationByHandleEx(FileIdInfo) failed.");
            ulong serial=unchecked((ulong)Marshal.ReadInt64(p,0));byte[] id=new byte[16];Marshal.Copy(IntPtr.Add(p,8),id,0,16);
            return new FileIdentity(serial,Convert.ToHexString(id).ToLowerInvariant());
        }
        finally{Marshal.FreeHGlobal(p);}
    }

    public static (FileRevision revision,bool directory,uint attributes) QueryFileRevision(IntPtr h)
    {
        IntPtr p=Marshal.AllocHGlobal(Marshal.SizeOf<FILE_BASIC_INFO>());
        try
        {
            if(!GetFileInformationByHandleEx(h,FileBasicInfo,p,(uint)Marshal.SizeOf<FILE_BASIC_INFO>()))throw Win32("native_error","GetFileInformationByHandleEx(FileBasicInfo) failed.");
            var b=Marshal.PtrToStructure<FILE_BASIC_INFO>(p);bool dir=(b.FileAttributes&0x10)!=0;long len=0;if(!dir&&!GetFileSizeEx(h,out len))throw Win32("native_error","GetFileSizeEx failed.");
            return(new FileRevision(len,b.LastWriteTime),dir,b.FileAttributes);
        }
        finally{Marshal.FreeHGlobal(p);}
    }

    public static VerifiedFileHandle OpenVerifiedFile(FileConcept file,bool write=false,string? expectedRevision=null)
    {
        IntPtr h=OpenPath(file.Path,write,false);
        try
        {
            var id=QueryFileIdentity(h);if(id!=file.Identity)throw new ShellEyeException("stale","Current path resolves to a different physical file object.");
            var rev=QueryFileRevision(h).revision;if(expectedRevision!=null&&!StringComparer.Ordinal.Equals(rev.ToString(),expectedRevision))throw new ShellEyeException("stale","File revision precondition failed.");
            return new VerifiedFileHandle(h,file.Id,file.Path,id,rev);
        }
        catch{CloseHandle(h);throw;}
    }

    public static FileRevision WriteVerifiedFile(FileConcept file,string text,string? expectedRevision)
    {
        using var vh=OpenVerifiedFile(file,true,expectedRevision);
        byte[] data=new UTF8Encoding(false).GetBytes(text);
        if(!SetFilePointerEx(vh.Handle,0,out _,FILE_BEGIN))throw Win32("native_error","SetFilePointerEx failed.");
        if(data.Length>0&&(!WriteFile(vh.Handle,data,(uint)data.Length,out uint written,IntPtr.Zero)||written!=data.Length))throw Win32("native_error","WriteFile failed.");
        if(!SetEndOfFile(vh.Handle))throw Win32("native_error","SetEndOfFile failed.");
        return QueryFileRevision(vh.Handle).revision;
    }

    public static string ReadVerifiedFile(FileConcept file,int maxBytes=1024*1024)
    {
        using var vh=OpenVerifiedFile(file,false,null);long len=vh.Revision.Length;if(len>maxBytes)throw new ShellEyeException("busy","Requested content exceeds bounded read.");
        byte[] b=new byte[(int)len];if(!SetFilePointerEx(vh.Handle,0,out _,FILE_BEGIN))throw Win32("native_error","SetFilePointerEx failed.");
        int off=0;while(off<b.Length){byte[] chunk=new byte[b.Length-off];if(!ReadFile(vh.Handle,chunk,(uint)chunk.Length,out uint n,IntPtr.Zero))throw Win32("native_error","ReadFile failed.");if(n==0)break;Buffer.BlockCopy(chunk,0,b,off,(int)n);off+=(int)n;}
        return Encoding.UTF8.GetString(b,0,off);
    }

    public static void RenameVerifiedFile(FileConcept file,string newPath,bool replace=false,string? expectedRevision=null)
    {
        using var vh=OpenVerifiedFile(file,true,expectedRevision);byte[] name=Encoding.Unicode.GetBytes(Path.GetFullPath(newPath));IntPtr p=Marshal.AllocHGlobal(20+name.Length+2);
        try
        {
            for(int i=0;i<20+name.Length+2;i++)Marshal.WriteByte(p,i,0);Marshal.WriteByte(p,0,replace?(byte)1:(byte)0);Marshal.WriteIntPtr(p,8,IntPtr.Zero);Marshal.WriteInt32(p,16,name.Length);Marshal.Copy(name,0,IntPtr.Add(p,20),name.Length);
            if(!SetFileInformationByHandle(vh.Handle,FileRenameInfo,p,(uint)(20+name.Length+2)))throw Win32("native_error","SetFileInformationByHandle(FileRenameInfo) failed.");
        }
        finally{Marshal.FreeHGlobal(p);}
    }

    public static void DeleteVerifiedFile(FileConcept file,string? expectedRevision=null)
    {
        using var vh=OpenVerifiedFile(file,true,expectedRevision);
        if(!String.Equals(file.Kind,"dir",StringComparison.Ordinal))
        {
            IntPtr ex=Marshal.AllocHGlobal(sizeof(uint));
            try
            {
                Marshal.WriteInt32(ex,unchecked((int)(FILE_DISPOSITION_DELETE|FILE_DISPOSITION_POSIX_SEMANTICS)));
                if(SetFileInformationByHandle(vh.Handle,FileDispositionInfoEx,ex,sizeof(uint)))return;
                int error=Marshal.GetLastWin32Error();if(error is not (1 or 50 or 87))throw new ShellEyeException("native_error","SetFileInformationByHandle(FileDispositionInfoEx) failed.",error);
            }
            finally{Marshal.FreeHGlobal(ex);}
        }
        IntPtr p=Marshal.AllocHGlobal(1);try{Marshal.WriteByte(p,1);if(!SetFileInformationByHandle(vh.Handle,FileDispositionInfo,p,1))throw Win32("native_error","SetFileInformationByHandle(FileDispositionInfo) failed.");}finally{Marshal.FreeHGlobal(p);}
    }

    public static FileContinuity QueryFileContinuity(string path)
    {
        string root=Path.GetPathRoot(Path.GetFullPath(path))!;string drive=root.TrimEnd('\\');
        if(!drive.Equals("C:",StringComparison.OrdinalIgnoreCase))return new FileContinuity(null,null);
        IntPtr file=OpenPath(path,false,false);try
        {
            long? usn=QueryLastUsn(file);string? journal=QueryJournalId(drive);return new FileContinuity(journal,usn);
        }finally{CloseHandle(file);}
    }

    public static long? QueryLastUsn(IntPtr file)
    {
        IntPtr input=Marshal.AllocHGlobal(4),output=Marshal.AllocHGlobal(4096);
        try
        {
            Marshal.WriteInt16(input,0,2);Marshal.WriteInt16(input,2,4);
            if(!DeviceIoControl(file,FSCTL_READ_FILE_USN_DATA,input,4,output,4096,out uint bytes,IntPtr.Zero))return null;
            if(bytes<32)return null;ushort major=unchecked((ushort)Marshal.ReadInt16(output,4));int usnOffset=major>=3?40:24;if(bytes<usnOffset+8)return null;return Marshal.ReadInt64(output,usnOffset);
        }
        finally{Marshal.FreeHGlobal(input);Marshal.FreeHGlobal(output);}
    }

    public static string? QueryJournalId(string drive)
    {
        var sa=new SECURITY_ATTRIBUTES{nLength=Marshal.SizeOf<SECURITY_ATTRIBUTES>(),bInheritHandle=false};IntPtr h=CreateFileW(@"\\.\"+drive,GENERIC_READ,FILE_SHARE_READ|FILE_SHARE_WRITE,ref sa,OPEN_EXISTING,0,IntPtr.Zero);
        if(h==new IntPtr(-1))return null;IntPtr output=Marshal.AllocHGlobal(128);
        try{if(!DeviceIoControl(h,FSCTL_QUERY_USN_JOURNAL,IntPtr.Zero,0,output,128,out uint bytes,IntPtr.Zero)||bytes<8)return null;ulong id=unchecked((ulong)Marshal.ReadInt64(output,0));return "0x"+id.ToString("x16");}
        finally{Marshal.FreeHGlobal(output);CloseHandle(h);}
    }

    public static VolumeConcept QueryVolume(string id,string drive)
    {
        string root=drive.TrimEnd('\\')+"\\";var guid=new StringBuilder(128);if(!GetVolumeNameForVolumeMountPointW(root,guid,(uint)guid.Capacity))throw Win32("native_error","GetVolumeNameForVolumeMountPoint failed.");
        var fs=new StringBuilder(64);if(!GetVolumeInformationW(root,null,0,out uint serial,out _,out _,fs,(uint)fs.Capacity))throw Win32("native_error","GetVolumeInformation failed.");
        if(!GetDiskFreeSpaceExW(root,out _,out ulong total,out ulong free))throw Win32("native_error","GetDiskFreeSpaceEx failed.");
        return new VolumeConcept(id,drive.TrimEnd('\\'),guid.ToString(),fs.ToString(),serial,checked((long)total),checked((long)free));
    }
}

public sealed class VerifiedFileHandle : IDisposable
{
    private IntPtr _handle; public IntPtr Handle => _handle;public string FileId{get;}public string Path{get;}public FileIdentity Identity{get;}public FileRevision Revision{get;}
    internal VerifiedFileHandle(IntPtr h,string id,string path,FileIdentity identity,FileRevision rev){_handle=h;FileId=id;Path=path;Identity=identity;Revision=rev;}
    public void Dispose(){var h=Interlocked.Exchange(ref _handle,IntPtr.Zero);if(h!=IntPtr.Zero)WindowsNative.CloseHandle(h);}
}




