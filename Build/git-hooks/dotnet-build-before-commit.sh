set -e

TEMP_DIRECTORY=$(mktemp -d)
SRC_DIRECTORY=$(pwd)

git clone --depth 1 .git $TEMP_DIRECTORY
git diff -P --cached | patch -p1 -d $TEMP_DIRECTORY
pushd $TEMP_DIRECTORY
dotnet test
popd
rm -rf $TEMP_DIRECTORY