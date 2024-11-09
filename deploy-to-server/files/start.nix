{ pkgs ? import <nixpkgs> {} }:

(pkgs.buildFHSEnv {
  name = "dotnet-env";
  targetPkgs = pkgs: (with pkgs; [
    gcc
    zlib
    icu
    openssl
  ]);
  runScript = "./web-backend";
}).env
