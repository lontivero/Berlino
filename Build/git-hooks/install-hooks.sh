GIT_DIRECTORY="../../.git"
GIT_HOOKS_DIRECTORY=""$GIT_DIRECTORY/hooks""
cp -f \
   dotnet-build-before-commit.sh \
   remove-trailing-whitespaces-before-commit.sh \
   pre-commit $GIT_HOOKS_DIRECTORY/

chmod +x \
   $GIT_HOOKS_DIRECTORY/dotnet-build-before-commit.sh \
   $GIT_HOOKS_DIRECTORY/remove-trailing-whitespaces-before-commit.sh \
   $GIT_HOOKS_DIRECTORY/pre-commit
