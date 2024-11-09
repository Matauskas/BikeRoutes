const std = @import("std");
const libssh2 = @import("libs/libssh2/build.zig");
const mbedtls = @import("libs/mbedtls/build.zig");

pub fn build(b: *std.Build) void {
    const target = b.standardTargetOptions(.{});
    const optimize = b.standardOptimizeOption(.{});

    const exe = b.addExecutable(.{
        .name = "deploy-to-server",
        .root_source_file = .{ .path = "src/main.zig" },
        .target = target,
        .optimize = optimize,
    });
    const mbedtls_lib = mbedtls.create(b, target, optimize);
    const ssh2_lib = libssh2.create(b, target, optimize);
    mbedtls_lib.link(ssh2_lib.step);
    ssh2_lib.link(exe);

    // TODO:
    // const git_submodule_run = b.addSystemCommand(&.{"git", "submodule", "update", "--init", "--recursive"});
    // exe.step.dependOn(&git_submodule_run.step);

    b.installArtifact(exe);

    const run_cmd = b.addRunArtifact(exe);
    run_cmd.step.dependOn(b.getInstallStep());

    if (b.args) |args| {
        run_cmd.addArgs(args);
    }

    const run_step = b.step("run", "Run the app");
    run_step.dependOn(&run_cmd.step);
}
