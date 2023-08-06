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
        git-hooks = pkgs.writeShellScriptBin "pre-commit" ''
          set -e
          IFS="
          "

          for line in $(${pkgs.git}/bin/git diff --check --cached | sed '/^[+-]/d') ; do
            FILE="$(echo "$line" | sed -r 's/:[0-9]+: .*//')"
            mv -f "$FILE" "$FILE.save"
            ${pkgs.git}/bin/git checkout -- "$FILE"
            sed -i 's/[[:space:]]*$//' "$FILE"
            ${pkgs.git}/bin/git add "$FILE"
            sed 's/[[:space:]]*$//' "$FILE.save" > "$FILE"
            rm "$FILE.save"
          done

          if [ "--$(git status -s | grep '^[A|D|M]')--" = "----" ]; then
            # empty commit
            echo
            echo -e "\033[31mNO CHANGES ADDED, ABORT COMMIT!\033[0m"
            exit 1
          fi

          TEMP_DIRECTORY=$(mktemp -d)
          SRC_DIRECTORY=$(pwd)

          ${pkgs.git}/bin/git clone .git $TEMP_DIRECTORY
          ${pkgs.git}/bin/git diff -P --cached | patch -p1 -d $TEMP_DIRECTORY
          pushd $TEMP_DIRECTORY
          ${pkgs.dotnet-sdk_7}/bin/dotnet test
          popd
          rm -rf $TEMP_DIRECTORY
          '';
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
              nuget-to-nix
              git-hooks
              ];

            DOTNET_ROOT = "${dotnet-sdk_7}";

            shellHook = ''
              ln -f -s ${git-hooks}/bin/pre-commit .git/hooks/pre-commit
            '';
         };
        };
      });
}
