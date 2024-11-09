const std = @import("std");
const ssh2 = @import("./ssh2.zig");
const Allocator = std.mem.Allocator;
const path = std.fs.path;

// TODO: Collect output and only show it if compilation failed.

const StringList = std.ArrayList([]u8);
fn deinitStringList(strings: StringList) void {
    const allocator = strings.allocator;
    for (strings.items) |file| {
        allocator.free(file);
    }
    strings.deinit();
}

const FileList = struct {
    dir: []u8,
    files: StringList,

    fn deinit(self: FileList) void {
        const allocator = self.files.allocator;
        allocator.free(self.dir);
        for (self.files.items) |file| {
            allocator.free(file);
        }
        self.files.deinit();
    }
};

fn listFiles(allocator: Allocator, dir: []const u8) !FileList {
    var files = StringList.init(allocator);
    errdefer deinitStringList(files);

    var iter_output_dir = try std.fs.openIterableDirAbsolute(dir, .{});
    defer iter_output_dir.close();

    var walker = try iter_output_dir.walk(allocator);
    defer walker.deinit();
    while (try walker.next()) |entry| {
        if (entry.kind != .file) continue;
        try files.append(try allocator.dupe(u8, entry.path));
    }

    return FileList{
        .dir = try allocator.dupe(u8, dir),
        .files = files
    };
}

fn compileBackend(allocator: Allocator, dir: []const u8) !FileList {
    const runtime = "ubuntu.20.04-x64";
    var backend_process = std.process.Child.init(
        &[_][]const u8{
            "dotnet",
            "publish",
            "./web-backend.csproj",
            "--runtime",
            runtime,
            "--self-contained",
            "--configuration",
            "Release"
        },
        allocator
    );

    backend_process.cwd = dir;

    const exit_code = try backend_process.spawnAndWait();
    if (exit_code != .Exited or exit_code.Exited != 0) {
        return error.CompilationFailed;
    }

    var files = StringList.init(allocator);
    errdefer deinitStringList(files);

    const output_dir = try path.join(allocator, &[_][]const u8 { dir, "bin", "Release", "net6.0", runtime, "publish" });
    errdefer allocator.free(output_dir);

    var iter_output_dir = try std.fs.openIterableDirAbsolute(output_dir, .{});
    defer iter_output_dir.close();

    var walker = try iter_output_dir.walk(allocator);
    defer walker.deinit();
    while (try walker.next()) |entry| {
        if (entry.kind != .file) continue;
        if (std.mem.eql(u8, entry.path, "appsettings.Development.json")) continue;
        if (std.mem.endsWith(u8, entry.path, ".pdb")) continue;

        try files.append(try allocator.dupe(u8, entry.path));
    }

    return FileList{
        .dir = output_dir,
        .files = files
    };
}

fn compileFrontend(allocator: Allocator, dir: []const u8) !FileList {
    { // npm i
        var install_process = std.process.Child.init(
            &[_][]const u8{ "npm", "i" },
            allocator
        );
        install_process.cwd = dir;

        const exit_code = try install_process.spawnAndWait();
        if (exit_code != .Exited or exit_code.Exited != 0) {
            return error.CompilationFailed;
        }
    }

    { // npm run build
        var build_process = std.process.Child.init(
            &[_][]const u8{ "npm", "run", "build" },
            allocator
        );
        build_process.cwd = dir;

        const exit_code = try build_process.spawnAndWait();
        if (exit_code != .Exited or exit_code.Exited != 0) {
            return error.CompilationFailed;
        }
    }

    const output_dir = try path.join(allocator, &[_][]const u8{ dir, "build" });
    defer allocator.free(output_dir);

    return listFiles(allocator, output_dir);
}

fn ignoreNoSuchFile(err: anyerror!void) !void {
    if (err != error.SftpNoSuchFile) {
        return err;
    }
}

fn megabytes(count: u32) u32 {
    return count * 1024 * 1024;
}

fn mkdirRecursive(sftp: *ssh2.Sftp, dir: []const u8) !void {
    const dir_mode = 0o0755;
    var stack = std.BoundedArray([]const u8, 16).init(0) catch unreachable;

    var current_dir = dir;
    while (true) {
        if (sftp.mkdir(current_dir, dir_mode)) {
            break;
        } else |err| {
            if (err == error.SftpFailure) {
                break; // Already exists
            }
            if (err != error.SftpNoSuchFile) {
                return err;
            }

            try stack.append(current_dir);
            const dirname = path.dirnamePosix(current_dir);
            if (dirname == null) {
                return error.Failed;
            }
            current_dir = dirname.?;
        }
    }

    while (stack.popOrNull()) |saved_dir| {
        try sftp.mkdir(saved_dir, dir_mode);
    }
}

fn uploadFile(
    ssh: *ssh2.Session,
    sftp: *ssh2.Sftp,
    allocator: Allocator,
    local_filename: []const u8,
    remote_filename: [:0]const u8,
    mode: u32
) !void {
    try ignoreNoSuchFile(sftp.rmfile(remote_filename));

    if (path.dirnamePosix(remote_filename)) |dir| {
        try mkdirRecursive(sftp, dir);
    }

    const local_file = try std.fs.openFileAbsolute(local_filename, .{});
    defer local_file.close();
    const file_contents = try local_file.readToEndAlloc(allocator, megabytes(256));
    defer allocator.free(file_contents);
    try ssh.sendFile(remote_filename, file_contents, mode);
}

fn isFileElf(absolute_path: []const u8) !bool {
    const file = try std.fs.openFileAbsolute(absolute_path, .{});
    defer file.close();

    var buffer: [4]u8 = undefined;
    const bytes_read = try file.readAll(&buffer);
    return std.mem.eql(u8, buffer[0..bytes_read], &.{ 0x7f, 0x45, 0x4c, 0x46 });
}

fn uploadFiles(ssh: *ssh2.Session, file_list: FileList, destination_dir: []const u8) !void {
    const allocator = ssh.allocator;

    var sftp = try ssh2.Sftp.init(ssh);
    defer sftp.deinit();

    for (file_list.files.items) |filename| {
        const local_path = try path.join(allocator, &.{ file_list.dir, filename });
        defer allocator.free(local_path);

        const remote_filename = try allocator.dupe(u8, filename);
        defer allocator.free(remote_filename);
        std.mem.replaceScalar(u8, remote_filename, '\\', '/');

        const remote_path = try std.mem.joinZ(allocator, "/", &.{ destination_dir, remote_filename });
        defer allocator.free(remote_path);

        var mode: u32 = 0o644;
        if (try isFileElf(local_path)) {
            mode = 0o755;
        }

        try uploadFile(ssh, &sftp, allocator, local_path, remote_path, mode);
    }
}

pub fn main() !void {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    var allocator = gpa.allocator();
    defer _ = gpa.deinit();

    var args = try std.process.argsAlloc(allocator);
    defer std.process.argsFree(allocator, args);

    var upload_backend = true;
    var upload_frontend = true;

    // TODO: Add command to delete database
    if (args.len > 1) {
        if (std.mem.eql(u8, args[1], "backend")) {
            upload_frontend = false;
        } else if (std.mem.eql(u8, args[1], "frontend")) {
            upload_backend = false;
        } else {
            std.log.err("Unknown command: {s}\n", .{args[1]});
            return;
        }
    }

    try ssh2.init();
    defer ssh2.deinit();

    var cwd = std.fs.cwd();

    const remote_base_folder = "/home/komanda-x/website";
    const public_key_filename = try cwd.realpathAlloc(allocator, "komanda-x.pub");
    defer allocator.free(public_key_filename);
    const private_key_filename = try cwd.realpathAlloc(allocator, "komanda-x");
    defer allocator.free(private_key_filename);

    const remote_wwwroot_folder = try std.mem.join(allocator, "/", &.{ remote_base_folder, "wwwroot" });
    defer allocator.free(remote_wwwroot_folder);

    var ssh = try ssh2.Session.init(allocator);
    defer _ = ssh.deinit();

    try ssh.connect("rpuzonas.com", 22);
    try ssh.authPublicPrivateKeyFile("komanda-x", public_key_filename, private_key_filename, null);

    // TODO: Add progress bar when uploading files

    var backend_files: ?FileList = null;
    defer if (backend_files) |files| files.deinit();

    var frontend_files: ?FileList = null;
    defer if (frontend_files) |files| files.deinit();

    if (upload_backend) {
        std.log.info("Compiling backend", .{});
        var backend_dir = try cwd.realpathAlloc(allocator, "../web-backend");
        defer allocator.free(backend_dir);
        backend_files = try compileBackend(allocator, backend_dir);
    }

    if (upload_frontend) {
        std.log.info("Compiling frontend", .{});
        var frontend_dir = try cwd.realpathAlloc(allocator, "../web-frontend");
        defer allocator.free(frontend_dir);
        frontend_files = try compileFrontend(allocator, frontend_dir);
    }

    if (backend_files) |files| {
        std.log.info("Uploading backend files", .{});
        try uploadFiles(&ssh, files, remote_base_folder);
    }

    if (frontend_files) |files| {
        std.log.info("Uploading frontend files", .{});
        try uploadFiles(&ssh, files, remote_wwwroot_folder);
    }

    std.log.info("Uploading general files", .{});
    const general_files_dir = try cwd.realpathAlloc(allocator, "files");
    defer allocator.free(general_files_dir);
    const general_files = try listFiles(allocator, general_files_dir);
    defer general_files.deinit();
    try uploadFiles(&ssh, general_files, remote_base_folder);

    if (upload_backend) {
        std.log.info("Restarting server", .{});
        try ssh.exec("systemctl --user daemon-reload");
        try ssh.exec("systemctl --user restart website-x.service");
        // TODO: Wait until website finishes restarting
    }

    std.log.info("Done", .{});
}
