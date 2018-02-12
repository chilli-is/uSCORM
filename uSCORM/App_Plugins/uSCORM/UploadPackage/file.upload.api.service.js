angular.module("umbraco.resources")
    .factory("fileUploadService", function ($http) {
        return {
            uploadFileToServer: function (file, nodeId) {
                var request = {
                    file: file,
                    nodeId: nodeId
                };
                return $http({
                    method: 'POST',
                    url: "/Umbraco/Api/uSCORM/UploadFileToServer",
                    headers: { 'Content-Type': undefined },
                    transformRequest: function (data) {
                        var formData = new FormData();
                        formData.append("file", data.file);
                        formData.append("nodeId", data.nodeId);
                        return formData;
                    },
                    data: request
                }).then(function (response) {
                    if (response) {
                        var fileName = response.data;
                        return fileName;
                    } else {
                        return false;
                    }
                });
            }
        };
    });