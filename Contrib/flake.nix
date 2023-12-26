{
  description = "A simple and deterministic bitcoin wallet for self custody";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = {
    self,
    nixpkgs,
    flake-utils,
  }:
    flake-utils.lib.eachDefaultSystem (system: let
      pkgs = import nixpkgs { inherit system; config.allowUnfree = true; };
      code-coverage-report = pkgs.callPackage ./coverage.nix {};
      git-hooks = pkgs.callPackage ./trailing-spaces.nix {};
    in {
      packages = {
        default = self.packages.${system}.berlino-wallet;

        berlino-wallet = pkgs.callPackage ./default.nix {
          dotnet-sdk = pkgs.dotnet-sdk_8;
          dotnet-runtime = pkgs.dotnet-runtime_8;
        };
      };

      devShells = with pkgs; {
        default = mkShell {
          name = "berlino-shell";
          packages = [
            dotnet-sdk_8
            nuget-to-nix
            sqlite-interactive
            bitcoin
            tor
            websocat
            git-hooks
            code-coverage-report
            zlib # Aot
            jetbrains.rider
          ];

          DOTNET_ROOT = "${dotnet-sdk_8}";

          shellHook = ''
            export DOTNET_CLI_TELEMETRY_OPTOUT=1
            export DOTNET_NOLOGO=1
            export GIT_TOP_LEVEL="$(${pkgs.git}/bin/git rev-parse --show-toplevel)"
            ln -f -s ${git-hooks}/bin/pre-commit $GIT_TOP_LEVEL/.git/hooks/pre-commit
            export PS1='\n\[\033[1;34m\][Berlino:\w]\$\[\033[0m\] '
          '';
        };
      };
    });
}
