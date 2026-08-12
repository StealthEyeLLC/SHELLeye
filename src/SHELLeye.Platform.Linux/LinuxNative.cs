using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SHELLeye.Platform.Linux;

internal static unsafe class LinuxNative
{
    internal const int AT_FDCWD=-100,AT_EMPTY_PATH=0x1000;
    internal const int O_RDONLY=0,O_RDWR=2,O_CLOEXEC=0x80000,O_DIRECTORY=0x10000,O_PATH=0x200000;
    internal const short POLLIN=0x0001;
    internal const uint STATX_BASIC_STATS=0x000007ff,STATX_BTIME=0x00000800,STATX_MNT_ID=0x00001000,STATX_MNT_ID_UNIQUE=0x00004000;
    [StructLayout(LayoutKind.Sequential)] internal struct PollFd{public int fd;public short events;public short revents;}
    [StructLayout(LayoutKind.Sequential)] internal struct StatxTimestamp{public long tv_sec;public uint tv_nsec;public int reserved;}
    [StructLayout(LayoutKind.Sequential)] internal unsafe struct Statx
    {
        public uint stx_mask,stx_blksize;public ulong stx_attributes;public uint stx_nlink,stx_uid,stx_gid;public ushort stx_mode,spare0;public ulong stx_ino,stx_size,stx_blocks,stx_attributes_mask;
        public StatxTimestamp stx_atime,stx_btime,stx_ctime,stx_mtime;public uint stx_rdev_major,stx_rdev_minor,stx_dev_major,stx_dev_minor;public ulong stx_mnt_id;public uint stx_dio_mem_align,stx_dio_offset_align;public ulong stx_subvol;public uint stx_atomic_write_unit_min,stx_atomic_write_unit_max,stx_atomic_write_segments_max,stx_dio_read_offset_align;public fixed ulong spare3[9];
    }
    [DllImport("libc",SetLastError=true)] internal static extern uint getuid();
    [DllImport("libc",SetLastError=true)] internal static extern uint geteuid();
    [DllImport("libc",SetLastError=true)] internal static extern uint getgid();
    [DllImport("libc",SetLastError=true)] internal static extern uint getegid();
    [DllImport("libc",SetLastError=true)] internal static extern int getgroups(int size,[Out] uint[] list);
    [DllImport("libc",SetLastError=true)] internal static extern int close(int fd);
    [DllImport("libc",SetLastError=true,CharSet=CharSet.Ansi)] internal static extern int open(string pathname,int flags,uint mode=0);
    [DllImport("libc",SetLastError=true)] internal static extern int poll([In,Out] PollFd[] fds,uint nfds,int timeout);
    [DllImport("libc",SetLastError=true,EntryPoint="pidfd_open")] internal static extern int pidfd_open(int pid,uint flags);
    [DllImport("libc",SetLastError=true,EntryPoint="pidfd_send_signal")] internal static extern int pidfd_send_signal(int pidfd,int sig,IntPtr info,uint flags);
    [DllImport("libc",SetLastError=true,CharSet=CharSet.Ansi)] internal static extern int statx(int dirfd,string pathname,int flags,uint mask,out Statx statxbuf);
    [DllImport("libc",SetLastError=true)] internal static extern int inotify_init1(int flags);
    [DllImport("libc",SetLastError=true,CharSet=CharSet.Ansi)] private static extern long readlink(string path,byte[] buffer,ulong bufsiz);
    [DllImport("libc",SetLastError=true,CharSet=CharSet.Ansi)] private static extern int name_to_handle_at(int dirfd,string pathname,IntPtr handle,ref int mount_id,int flags);
    [DllImport("libc",SetLastError=true)] private static extern int open_by_handle_at(int mount_fd,IntPtr handle,int flags);
    [DllImport("libc",SetLastError=true,CharSet=CharSet.Ansi)] internal static extern int execv(string path,IntPtr argv);
    internal static int Errno=>Marshal.GetLastWin32Error();
    internal static string ErrnoMessage(int e)=>new Win32Exception(e).Message;
    internal static string ReadLink(string path){var b=new byte[4096];long n=readlink(path,b,(ulong)b.Length);if(n<0)throw new ProviderException("native_error",$"readlink failed: {path}",Errno);return Encoding.UTF8.GetString(b,0,checked((int)n));}
    internal static uint[] Groups(){int n=getgroups(0,Array.Empty<uint>());if(n<=0)return Array.Empty<uint>();var g=new uint[n];int a=getgroups(g.Length,g);if(a<0)throw new ProviderException("native_error","getgroups failed",Errno);return a==g.Length?g:g.Take(a).ToArray();}
    internal sealed record ProcInfo(int Pid,int Ppid,ulong StartTicks,string Name,string? Exe);
    internal static ProcInfo ReadProc(int pid)
    {
        string text;try{text=File.ReadAllText($"/proc/{pid}/stat");}catch(FileNotFoundException){throw new ProviderException("destroyed","Process is gone.");}catch(DirectoryNotFoundException){throw new ProviderException("destroyed","Process is gone.");}
        int l=text.IndexOf('('),r=text.LastIndexOf(')');if(l<0||r<=l)throw new ProviderException("native_error","Malformed /proc stat record.");string name=text[(l+1)..r];string[] tail=text[(r+1)..].Trim().Split(' ',StringSplitOptions.RemoveEmptyEntries);if(tail.Length<20||!int.TryParse(tail[1],out int ppid)||!ulong.TryParse(tail[19],out ulong start))throw new ProviderException("native_error","Malformed /proc stat fields.");string? exe=null;try{exe=ReadLink($"/proc/{pid}/exe");}catch{}return new(pid,ppid,start,name,exe);
    }
    internal sealed class VerifiedPidfd:IDisposable{public int Fd{get;}public ProcInfo Process{get;}public VerifiedPidfd(int fd,ProcInfo p){Fd=fd;Process=p;}public void Dispose(){if(Fd>=0)close(Fd);}}
    internal static bool IsPidfdExited(int fd){var f=new[]{new PollFd{fd=fd,events=POLLIN}};int rc=poll(f,1,0);if(rc<0)throw new ProviderException("native_error","poll(pidfd) failed",Errno);return rc>0&&f[0].revents!=0;}
    internal static VerifiedPidfd OpenVerifiedPidfd(int pid,ulong? expected,bool allowExited=false)
    {
        int fd;try{fd=pidfd_open(pid,0);}catch(EntryPointNotFoundException e){throw new ProviderException("unsupported_by_provider","pidfd_open is unavailable.",inner:e);}if(fd<0){int e=Errno;if(e is 2 or 3)throw new ProviderException("destroyed","Process is gone.",e);if(e is 1 or 13)throw new ProviderException("permission_denied","pidfd_open permission denied.",e);throw new ProviderException("native_error","pidfd_open failed.",e);}
        try{if(!allowExited&&IsPidfdExited(fd))throw new ProviderException("destroyed","Process already exited.");var p=ReadProc(pid);if(!allowExited&&IsPidfdExited(fd))throw new ProviderException("destroyed","Process exited during verification.");if(expected.HasValue&&p.StartTicks!=expected.Value)throw new ProviderException("stale","PID now refers to a different process lifetime.",details:new{pid,expectedStartTicks=expected,currentStartTicks=p.StartTicks});return new(fd,p);}catch{close(fd);throw;}
    }
    internal static bool WaitPidfd(int fd,int timeoutMs){var f=new[]{new PollFd{fd=fd,events=POLLIN}};int rc=poll(f,1,timeoutMs);if(rc<0)throw new ProviderException("native_error","poll(pidfd) failed",Errno);return rc>0&&f[0].revents!=0;}
    internal sealed record StatWitness(uint Mask,uint DevMajor,uint DevMinor,ulong Inode,ulong MountId,bool UniqueMountId,ulong Size,long MTimeNs,long CTimeNs,long? BTimeNs,uint Mode,uint Uid,uint Gid);
    internal static long TimestampNs(StatxTimestamp t)=>checked(t.tv_sec*1_000_000_000L+t.tv_nsec);
    internal static StatWitness StatxPath(string path){uint req=STATX_BASIC_STATS|STATX_BTIME|STATX_MNT_ID|STATX_MNT_ID_UNIQUE;if(statx(AT_FDCWD,path,0,req,out var st)!=0){int e=Errno;if(e is 2 or 20)throw new ProviderException("not_found","File path is not current.",e);if(e is 1 or 13)throw new ProviderException("permission_denied","statx permission denied.",e);if(e==38)throw new ProviderException("unsupported_by_provider","statx is unavailable.",e);throw new ProviderException("native_error","statx failed.",e);}return FromStatx(st);}
    internal static StatWitness StatxFd(int fd){uint req=STATX_BASIC_STATS|STATX_BTIME|STATX_MNT_ID|STATX_MNT_ID_UNIQUE;if(statx(fd,"",AT_EMPTY_PATH,req,out var st)!=0)throw new ProviderException("native_error","statx(fd) failed.",Errno);return FromStatx(st);}
    private static StatWitness FromStatx(Statx st){bool unique=(st.stx_mask&STATX_MNT_ID_UNIQUE)!=0;long? b=(st.stx_mask&STATX_BTIME)!=0?TimestampNs(st.stx_btime):null;return new(st.stx_mask,st.stx_dev_major,st.stx_dev_minor,st.stx_ino,st.stx_mnt_id,unique,st.stx_size,TimestampNs(st.stx_mtime),TimestampNs(st.stx_ctime),b,st.stx_mode,st.stx_uid,st.stx_gid);}
    internal sealed record ExportedHandle(int Type,string BytesBase64,int MountId);
    internal static ExportedHandle? TryNameToHandle(string path)
    {
        int capacity=128;for(int attempt=0;attempt<2;attempt++){IntPtr p=Marshal.AllocHGlobal(8+capacity);try{Marshal.WriteInt32(p,capacity);Marshal.WriteInt32(p,4,0);int mountId=0;int rc=name_to_handle_at(AT_FDCWD,path,p,ref mountId,0);if(rc==0){int len=Marshal.ReadInt32(p),type=Marshal.ReadInt32(p,4);var bytes=new byte[len];Marshal.Copy(IntPtr.Add(p,8),bytes,0,len);return new(type,Convert.ToBase64String(bytes),mountId);}int e=Errno;if(e==75){int req=Marshal.ReadInt32(p);if(req>capacity&&req<=4096){capacity=req;continue;}}if(e is 1 or 13 or 22 or 38 or 95)return null;return null;}finally{Marshal.FreeHGlobal(p);}}return null;
    }
    internal sealed record MountInfo(int MountId,int ParentId,string MajorMinor,string Root,string MountPoint,string FsType,string Source);
    internal static IReadOnlyList<MountInfo> Mounts(){var list=new List<MountInfo>();foreach(string line in File.ReadLines("/proc/self/mountinfo")){int sep=line.IndexOf(" - ",StringComparison.Ordinal);if(sep<0)continue;string[] left=line[..sep].Split(' ',StringSplitOptions.RemoveEmptyEntries),right=line[(sep+3)..].Split(' ',StringSplitOptions.RemoveEmptyEntries);if(left.Length<5||right.Length<2)continue;if(!int.TryParse(left[0],out int id)||!int.TryParse(left[1],out int parent))continue;list.Add(new(id,parent,left[2],UnescapeMount(left[3]),UnescapeMount(left[4]),right[0],UnescapeMount(right[1])));}return list;}
    private static string UnescapeMount(string v)=>v.Replace("\\040"," ").Replace("\\011","\t").Replace("\\012","\n").Replace("\\134","\\");
    internal static int TryOpenByHandle(ExportedHandle handle,uint devMajor,uint devMinor,bool write,out int lastErrno)
    {
        byte[] bytes=Convert.FromBase64String(handle.BytesBase64);IntPtr p=Marshal.AllocHGlobal(8+bytes.Length);try{Marshal.WriteInt32(p,bytes.Length);Marshal.WriteInt32(p,4,handle.Type);Marshal.Copy(bytes,0,IntPtr.Add(p,8),bytes.Length);lastErrno=0;string dev=$"{devMajor}:{devMinor}";foreach(var mount in Mounts().Where(m=>m.MajorMinor==dev)){int mfd=open(mount.MountPoint,O_PATH|O_CLOEXEC|O_DIRECTORY);if(mfd<0){lastErrno=Errno;continue;}try{int fd=open_by_handle_at(mfd,p,(write?O_RDWR:O_RDONLY)|O_CLOEXEC);if(fd>=0)return fd;lastErrno=Errno;}finally{close(mfd);}}return -1;}finally{Marshal.FreeHGlobal(p);}
    }
    internal static string WorldEpoch(string providerKey,string bootId,string pidNamespace,ulong initStartTicks){byte[] data=Encoding.UTF8.GetBytes($"{providerKey}\n{bootId}\n{pidNamespace}\n{initStartTicks}");return "linux_epoch_"+Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant()[..24];}
}

internal sealed class ProviderException:Exception
{
    public string Code{get;}public int? NativeCode{get;}public object? Details{get;}
    public ProviderException(string code,string message,int? nativeCode=null,object? details=null,Exception? inner=null):base(message,inner){Code=code;NativeCode=nativeCode;Details=details;}
}
