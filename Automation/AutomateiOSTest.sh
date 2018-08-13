#!/bin/bash
{
# This script will compile, deploy and run the app,
# then retrieve the test report upon completion

# It requires 'ios-deploy' to be installed ( https://github.com/ios-control/ios-deploy )
# First install homebrew: /usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"
# Then install node: brew install node
# Then install iOS-deploy: npm install -g ios-deploy

PROJECT_PATH=../src/TestAppRunner/TestAppRunner.iOS/
PROJECT_TO_BUILD=${PROJECT_PATH}TestAppRunner.iOS.csproj
PLATFORM=iPhone
CONFIGURATON=Debug
BUNDLE_PATH=${PROJECT_PATH}/bin/${PLATFORM}/${CONFIGURATON}/TestAppRunner.iOS.app
BUNDLE_ID=com.companyname.TestAppRunner

# Compile iOS App:
msbuild /restore /t:Build ${PROJECT_TO_BUILD} /p:Configuraton=${CONFIGURATON} /p:Platform=${PLATFORM}

error_code=$?
if [ ${error_code} -eq 1 ]; then
   echo "Build failed"
   exit 1
fi

# Run the app
ios-deploy --args "-AutoRun true -ReportFile report.trx" --noninteractive --bundle "${BUNDLE_PATH}"

# Download the test report
/usr/local/bin/ios-deploy --debug --download=/Documents/ -bundle_id ${BUNDLE_ID} --to "/."
echo "Test run complete!"

# Uninstall the app
iOS-deploy --uninstall_only --bundle_id ${BUNDLE_ID}

}
