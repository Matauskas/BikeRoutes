const std = @import("std");
const Allocator = std.mem.Allocator;
const libssh2 = @cImport({
    @cInclude("libssh2.h");
    @cInclude("libssh2_sftp.h");
});

pub fn init() !void {
    const err = libssh2.libssh2_init(0);
    if (err != 0) {
        return error.InitFailed;
    }
}

pub fn deinit() void {
    libssh2.libssh2_exit();
}

extern "c" fn socket(domain: c_uint, sock_type: c_uint, protocol: c_uint) c_int;

fn decodeSshError(err: c_int) !void {
    if (err == 0) return;

    if (err == libssh2.LIBSSH2_ERROR_ALLOC) {
        return error.Alloc;
    } else if (err == libssh2.LIBSSH2_ERROR_SOCKET_SEND) {
        return error.SocketSend;
    } else if (err == libssh2.LIBSSH2_ERROR_SOCKET_TIMEOUT) {
        return error.SocketTimeout;
    } else if (err == libssh2.LIBSSH2_ERROR_AUTHENTICATION_FAILED) {
        return error.AuthenticationFailed;
    } else if (err == libssh2.LIBSSH2_ERROR_EAGAIN) {
        return error.EAGAIN;
    } else {
        return error.Unknown;
    }
}

pub const Channel = struct {
    handle: *libssh2.LIBSSH2_CHANNEL,

    pub fn exec(self: Channel, command: []const u8) !void {
        const request = "exec";
        const err = libssh2.libssh2_channel_process_startup(
            self.handle,
            request, request.len,
            command.ptr, @intCast(command.len)
        );
        try decodeSshError(err);
    }

    pub fn getExitStatus(self: Channel) i32 {
        return @intCast(libssh2.libssh2_channel_get_exit_status(self.handle));
    }

    pub fn close(self: Channel) void {
        _ = libssh2.libssh2_channel_close(self.handle);
        _ = libssh2.libssh2_channel_wait_closed(self.handle);
    }

    pub fn deinit(self: Channel) void {
        self.close();
        _ = libssh2.libssh2_channel_free(self.handle);
    }
};

pub const Session = struct {
    allocator: Allocator,
    handle: *libssh2.LIBSSH2_SESSION,
    socket: ?c_int = null,

    pub fn init(allocator: Allocator) !Session {
        const handle = libssh2.libssh2_session_init_ex(null, null, null, null);
        if (handle == null) {
            return error.InitFailed;
        }

        libssh2.libssh2_session_set_blocking(handle.?, 1);

        return Session{
            .allocator = allocator,
            .handle = handle.?
        };
    }

    pub fn connect(self: *Session, name: []const u8, port: u16) !void {
        const address_list = try std.net.getAddressList(self.allocator, name, port);
        defer address_list.deinit();
        if (address_list.addrs.len == 0) {
            return error.InvalidAddress;
        }

        const address = &address_list.addrs[0];
        var sockaddr: *const std.os.sockaddr = &address.any;

        var sockaddr_len: std.os.socklen_t = switch (address.any.family) {
            std.os.AF.INET => @sizeOf(std.os.sockaddr.in),
            std.os.AF.INET6 => @sizeOf(std.os.sockaddr.in6),
            std.os.AF.UNIX => @sizeOf(std.os.sockaddr.un),
            else => {
                return error.UnknownAddressType;
            }
        };

        const socket_fd: i32 = @intCast(socket(address.any.family, std.os.SOCK.STREAM, 0));
        if (socket_fd == 0) {
            return error.OpenSocketFailed;
        }
        errdefer _ = std.os.system.close(@ptrFromInt(@as(usize, @intCast(socket_fd))));

        const connect_err = std.os.system.connect(@ptrFromInt(@as(usize, @intCast(socket_fd))), sockaddr, sockaddr_len);
        if (connect_err != 0) {
            return error.ConnectFailed;
        }

        const handshake_err = libssh2.libssh2_session_handshake(self.handle, @intCast(socket_fd));
        if (handshake_err != 0) {
            return error.Handshake;
        }

        self.socket = socket_fd;
    }

    pub fn authPublicPrivateKey(
        self: *Session,
        username: [:0]const u8,
        public_key: [:0]const u8,
        private_key: [:0]const u8,
        passphrase: ?[*:0]const u8
    ) !void {
        const err = libssh2.libssh2_userauth_publickey_frommemory(
            self.handle,
            username, username.len,
            public_key, public_key.len,
            private_key, private_key.len,
            passphrase
        );
        try decodeSshError(err);
    }

    pub fn authPublicPrivateKeyFile(
        self: *Session,
        username: [:0]const u8,
        public_key_filename: []const u8,
        private_key_filename: []const u8,
        passphrase: ?[*:0]const u8
    ) !void {
        const public_key_file = try std.fs.openFileAbsolute(public_key_filename, .{});
        defer public_key_file.close();
        const public_key = try public_key_file.readToEndAllocOptions(self.allocator, 1024 * 1024, null, @alignOf(u8), 0);
        defer self.allocator.free(public_key);

        const private_key_file = try std.fs.openFileAbsolute(private_key_filename, .{});
        defer private_key_file.close();
        const private_key = try private_key_file.readToEndAllocOptions(self.allocator, 1024 * 1024, null, @alignOf(u8), 0);
        defer self.allocator.free(private_key);

        try self.authPublicPrivateKey(username, public_key, private_key, passphrase);
    }

    pub fn recvFile(self: *Session, path: [*:0]const u8) ![]u8 {
        var fileinfo: libssh2.libssh2_struct_stat = undefined;
        const maybe_channel = libssh2.libssh2_scp_recv2(self.handle, path, &fileinfo);

        if (maybe_channel == null) {
            return error.FailedToStart;
        }
        const channel = maybe_channel.?;
        defer _ = libssh2.libssh2_channel_free(channel);

        const file_size: usize = @intCast(fileinfo.st_size);

        var file_contents = try self.allocator.alloc(u8, file_size);
        errdefer self.allocator.free(file_contents);

        var total_read: usize = 0;
        while (total_read < file_size) {
            const left_amount = file_size - total_read;
            const read = libssh2.libssh2_channel_read(channel, file_contents[total_read..].ptr, left_amount);
            if (read < 0) {
                return error.ReadFailed;
            }
            total_read += @intCast(read);
        }

        return file_contents;
    }

    pub fn sendFile(self: *Session, path: [*:0]const u8, contents: []const u8, mode: u32) !void {
        const maybe_channel = libssh2.libssh2_scp_send(self.handle, path, @as(i32, @intCast(mode)), contents.len);
        if (maybe_channel == null) {
            const err = libssh2.libssh2_session_last_errno(self.handle);
            std.debug.print("{}\n", .{err});
            return error.FailedToStart;
        }
        const channel = maybe_channel.?;
        defer _ = libssh2.libssh2_channel_free(channel);

        defer _ = libssh2.libssh2_channel_wait_closed(channel);
        defer {
            _ = libssh2.libssh2_channel_send_eof(channel);
            _ = libssh2.libssh2_channel_wait_eof(channel);
        }

        var total_written: usize = 0;
        while (total_written < contents.len) {
            const left_amount = contents.len - total_written;
            const written = libssh2.libssh2_channel_write(channel, contents[total_written..].ptr, left_amount);
            if (written < 0) {
                return error.WriteFailed;
            }
            total_written += @intCast(written);
        }
    }

    pub fn openChannel(self: *Session) !Channel {
        const channel_type = "session";
        const channel_handle = libssh2.libssh2_channel_open_ex(
            self.handle,
            channel_type, channel_type.len,
            libssh2.LIBSSH2_CHANNEL_WINDOW_DEFAULT,
            libssh2.LIBSSH2_CHANNEL_PACKET_DEFAULT,
            null,
            0
        );
        if (channel_handle == null) {
            return error.Failed;
        }

        return Channel{
            .handle = channel_handle.?
        };
    }

    pub fn exec(self: *Session, command: []const u8) !void {
        const channel = try self.openChannel();
        defer channel.deinit();
        try channel.exec(command);
    }

    pub fn deinit(self: Session) bool {
        _ = libssh2.libssh2_session_disconnect(self.handle, "Normal Shutdown");

        // TODO: Automatically retry freeing?
        const err = libssh2.libssh2_session_free(self.handle);
        if (err == libssh2.LIBSSH2_ERROR_EAGAIN) {
            return false;
        }

        if (self.socket) |fd| {
            _ = std.os.system.shutdown(@ptrFromInt(@as(usize, @intCast(fd))), 2);
            _ = std.os.system.close(@ptrFromInt(@as(usize, @intCast(fd))));
        }

        return true;
    }
};

pub const FileStat = struct {
    flags: u64,
    filesize: u64,
    uid: u64,
    gid: u64,
    permissions: u64,
    atime: u64,
    mtime: u64,

    pub fn isLink(self: FileStat) bool {
        return libssh2.LIBSSH2_SFTP_S_ISLNK(self.permissions);
    }

    pub fn isFile(self: FileStat) bool {
        return libssh2.LIBSSH2_SFTP_S_ISREG(self.permissions);
    }

    pub fn isDir(self: FileStat) bool {
        return libssh2.LIBSSH2_SFTP_S_ISDIR(self.permissions);
    }

    pub fn isCharacterFile(self: FileStat) bool {
        return libssh2.LIBSSH2_SFTP_S_ISCHR(self.permissions);
    }

    pub fn isBlockFile(self: FileStat) bool {
        return libssh2.LIBSSH2_SFTP_S_ISBLK(self.permissions);
    }

    pub fn isFifo(self: FileStat) bool {
        return libssh2.LIBSSH2_SFTP_S_ISFIFO(self.permissions);
    }

    pub fn isSocket(self: FileStat) bool {
        return libssh2.LIBSSH2_SFTP_S_ISSOCK(self.permissions);
    }
};

pub const Sftp = struct {
    session: *Session,
    handle: *libssh2.LIBSSH2_SFTP,

    pub fn init(session: *Session) !Sftp {
        const handle = libssh2.libssh2_sftp_init(session.handle);
        if (handle == null) {
            return error.InitFailed;
        }

        return Sftp{
            .session = session,
            .handle = handle.?
        };
    }

    fn decodeSftpError(self: *Sftp, err: c_int) !void {
        if (err == libssh2.LIBSSH2_ERROR_SFTP_PROTOCOL) {
            const sftp_err = libssh2.libssh2_sftp_last_error(self.handle);
            if (sftp_err == libssh2.LIBSSH2_FX_FAILURE) {
                return error.SftpFailure;
            } else if (sftp_err == libssh2.LIBSSH2_FX_NO_SUCH_FILE) {
                return error.SftpNoSuchFile;
            } else {
                return error.SftpGenericError; // TODO: make this not generic, add if's for the other error codes
            }
        } else if (err != 0) {
            try decodeSshError(err);
        }
    }

    pub fn mkdir(self: *Sftp, path: []const u8, mode: u32) !void {
        const err = libssh2.libssh2_sftp_mkdir_ex(
            self.handle,
            path.ptr,
            @intCast(path.len),
            @as(i32, @intCast(mode))
        );
        try self.decodeSftpError(err);
    }

    pub fn rmdir(self: *Sftp, path: []const u8) !void {
        const err = libssh2.libssh2_sftp_rmdir_ex(self.handle, path.ptr, @intCast(path.len));
        try self.decodeSftpError(err);
    }

    pub fn rmfile(self: *Sftp, filename: []const u8) !void {
        const err = libssh2.libssh2_sftp_unlink_ex(self.handle, filename.ptr, @intCast(filename.len));
        try self.decodeSftpError(err);
    }

    pub fn getStat(self: *Sftp, path: []const u8) !FileStat {
        var attrs: libssh2.LIBSSH2_SFTP_ATTRIBUTES = undefined;
        const err = libssh2.libssh2_sftp_stat_ex(
            self.handle,
            path.ptr,
            @intCast(path.len),
            libssh2.LIBSSH2_SFTP_STAT,
            &attrs
        );
        try self.decodeSftpError(err);

        return FileStat{
            .flags = attrs.flags,
            .filesize = attrs.filesize,
            .uid = attrs.uid,
            .gid = attrs.gid,
            .permissions = attrs.permissions,
            .atime = attrs.atime,
            .mtime = attrs.mtime
        };
    }

    pub fn deinit(self: Sftp) void {
        _ = libssh2.libssh2_sftp_shutdown(self.handle);
    }
};