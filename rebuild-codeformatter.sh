VERBOSE=false
CODEFORMATTER=`pwd`

for key in "$@"; do
    case $key in
        -v|--verbose)
            VERBOSE=true
            shift # past argument
            ;;
        *) # unknown option 
            ;;
    esac
done

echo "Cleaning.."
rm -rf /usr/local/bin/CodeFormatter/
rm -rf $CODEFORMATTER/bin/CodeFormatter/Debug

echo "Building.."
if [ $VERBOSE == true ]; then
    msbuild $CODEFORMATTER/src/CodeFormatter.sln
else
    msbuild $CODEFORMATTER/src/CodeFormatter.sln > /dev/null
fi

if [ ! -f $CODEFORMATTER/bin/CodeFormatter/Debug/CodeFormatter.exe ]; then
    echo "Failed to build! Re-run with -v for output!"
else 
    echo "Copying.."
    cp -R $CODEFORMATTER/bin/CodeFormatter/Debug /usr/local/bin/CodeFormatter
    echo "Done!"
fi
