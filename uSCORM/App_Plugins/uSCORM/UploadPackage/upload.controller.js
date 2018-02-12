angular.module("umbraco").controller("uSCORM.UploadPackageController", function ($scope, $routeParams, editorState, dialogService, $location, contentResource, fileUploadService, notificationsService, contentResource) {

    $scope.file = false;
    $scope.isUploading = false;
    $scope.assetId = editorState.current.id;
    $scope.defaultAssetFile = "";
    
    $scope.getFullUrl = function () {
        var url = encodeURI($location.protocol() + "://" +
            $location.host() + ":" +
            $location.port() +
            "/uSCORM-Assets/" + $scope.assetId + "/" + $scope.model.value);
        
        return url;
    }

    $scope.fileSelected = function (files) {
        // In this case, files is just a single path/filename
        $scope.file = files;
    };

    $scope.uploadFile = function () {
        if (!$scope.isUploading) {
            
            if ($scope.file) {
                
                $scope.isUploading = true;

                fileUploadService.uploadFileToServer($scope.file, $scope.assetId)
                    .then(function (response) {
                        if (response) {
                            notificationsService.success("Success", "Package unzipped and saved.");
                        }
                        $scope.isUploading = false;
                        
                        contentResource.getById($scope.assetId)
                            .then(function (content) {
                                
                                for (var i = 0; i < content.properties.length; i++) {

                                    var property = content.properties[i];

                                    if (property.alias == "assetPackage") {
                                        $scope.model.value = property.value;
                                        break;
                                    }

                                }

                            });
                        
                        
                    }, function (reason) {
                        notificationsService.error("Error", "File import failed: " + reason.message);
                        $scope.isUploading = false;
                    });
            } else {
                notificationsService.error("Error", "You must select a file to upload");
                $scope.isUploading = false;
            }
        }
    };


    // if the editor has an id (this is the parent node id)
    // get the model value and split by |
    if (editorState.current.id !== undefined) {

        if ($scope.model.value !== null && $scope.model.value !== "") {

            //$scope.assetFolder = $scope.model.value.toString().split("|")[0];
            //$scope.defaultAssetFile = $scope.model.value;

        }

        // $routeParams.id contains node id
        /*
        console.log($scope.model);
        console.log($routeParams);
        console.log(editorState.current);
        */
    }

});