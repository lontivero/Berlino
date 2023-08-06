# Can be run with:
# nix-build -E 'let pkgs = import <nixpkgs> { }; in pkgs.callPackage ./default.nix {dotnet-sdk = pkgs.dotnet-sdk_7;}' -A passthru.fetch-deps
{
  lib
, buildDotnetModule
, stdenv
, libunwind
, libuuid
, icu
, openssl
, zlib
, curl
, dotnet-sdk
, dotnet-runtime
}:
buildDotnetModule rec {
    inherit dotnet-sdk dotnet-runtime;

    pname = "berlino-wallet";
    version = "0.0.1";
    nugetDeps = ./deps.nix;

    src = ./.;

    projectFile = "Berlino.sln";
    testProjectFile = "Berlino.Tests/Berlino.Tests.fsproj";
    executables = [ "FilterBuilder" ];

    doCheck = true;

    meta = with lib; {
      homepage = "some_homepage";
      description = "some_description";
      license = licenses.mit;
    };
}
