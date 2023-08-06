{
  description = "A simple and deterministic bitcoin wallet for self custody";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = nixpkgs.legacyPackages.${system};
      in {
        packages = {
          default = self.packages.${system}.berlino-wallet;

          berlino-wallet = pkgs.callPackage ./default.nix {
            dotnet-sdk = pkgs.dotnet-sdk_7;
            dotnet-runtime = pkgs.dotnet-runtime_7;
          };
        };
        devShells = with pkgs; {
          default = mkShell {
            name = "berlino-shell";
            packages = [
              dotnet-sdk_7
              nuget-to-nix];

            DOTNET_ROOT = "${dotnet-sdk_7}";
            #LD_LIBRARY_PATH = "${lib.makeLibraryPath libs}";
            DOTNET_GLOBAL_TOOLS_PATH = "${builtins.getEnv "HOME"}/.dotnet/tools";

            shellHook = ''
              export PATH="$PATH:$DOTNET_GLOBAL_TOOLS_PATH"
            '';
         };
        };
      });
}
