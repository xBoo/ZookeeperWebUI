var setting = {
    view: {
        addHoverDom: addHoverDom,
        selectedMulti: false
    },
    edit: {
        enable: false,
        showRemoveBtn: false,
        showRenameBtn: false
    },
    data: {
        keep: {
            parent: true,
            leaf: true
        },
        simpleData: {
            enable: true
        }
    },
    callback: {
        beforeExpand: beforeExpand,
        onClick: OnClick
    }
};

$(document).ready(function () {

    $("#btnEdit").bind("click", function () {
        var path = $("#lb-path").text();
        var value;
        if ($("#text").hasClass("active"))
            value = $("#lb-value").val();
        else
            value = $("#lb-jsonvalue").val();

        var url = "/Home/UpdateNode";
        $.ajax({
            url: url,
            type: "Post",
            data: {
                path: path,
                value: value
            },
            success: function (data) {
                if (data.businessCode !== -1) {
                    alert("success!");
                } else
                    alert(data.businessMessage);
            },
            error: function (data) {
                alert("error");
            }
        });
    });

    $("#btnDelete").bind("click", function () {

        var returnVal = confirm("Are you sure？");
        if (returnVal === false) {
            return;
        }

        var path = $("#lb-path").text();
        var url = "/Home/DeleteNode?path=" + path;
        $.ajax({
            url: url,
            type: "GET",
            datatype: "Json",
            success: function (data) {
                if (data.businessCode !== -1) {
                    init();
                } else
                    alert(data.businessMessage);
            },
            error: function (data) {
                alert("error");
            }
        });
    });

    $("#btnAddModel").bind("click", function () {
        var path = $("#edit-path").val();
        var name = $("#edit-name").val();
        var value = $("#edit-value").val();

        if (name === "") {
            alert("name can not be null")
            return;
        }
        $.ajax({
            url: "/Home/CreateNode",
            type: "Post",
            datatype: "Json",
            data: {
                path: path + "/" + name,
                value: value,
                createModel: "PERSISTENT"
            },
            success: function (data) {
                if (data.businessCode !== -1) {
                    init();
                } else
                    alert(data.businessMessage);
            },
            error: function (data) {
                alert("error");
            }
        });

    });

    init();
});

function init() {
    var url = "/Home/GetNodes?path=/";
    $.ajax({
        url: url,
        type: "GET",
        datatype: "Json",
        success: function (data) {
            if (data.businessCode !== -1) {
                $.fn.zTree.init($("#zktree"), setting, data.returnObj);
            } else
                alert(data.businessMessage);
        },
        error: function (data) {
            alert("error");
        }
    });
};

function OnClick(event, treeId, treeNode) {
    var path = treeNode.bakValue;
    $("#config-address").html(path);

    showNode(treeNode);

    if (treeNode.open === false) {
        expandNodes(treeNode);
    } else {
        var treeObj = $.fn.zTree.getZTreeObj("zktree");
        treeObj.expandNode(treeNode, false, true, true);
    }
};

function beforeExpand(treeId, treeNode) {
    expandNodes(treeNode);
    return (treeNode.expand !== false);
};

function expandNodes(treeNode) {
    if (treeNode == null) {
        var treeObj = $.fn.zTree.getZTreeObj("zktree");
        treeNode = treeObj.getNodes()[0];
    }

    var path = treeNode.bakValue;
    var url = "/Home/GetNodes?path=" + path + "&parentId=" + treeNode.id;

    $.ajax({
        url: url,
        type: "GET",
        datatype: "Json",
        success: function (data) {
            if (data.businessCode !== -1) {
                add(treeNode, data.returnObj);
            } else
                alert(data.Message);
        },
        error: function (data) {
            alert("服务器访问错误");
        }
    });
}

function add(node, data) {
    var zTree = $.fn.zTree.getZTreeObj("zktree");

    if (node) {
        zTree.removeChildNodes(node);
        zTree.addNodes(node, data);
    }
};

function showNode(treeNode) {
    if (treeNode == null) {
        var treeObj = $.fn.zTree.getZTreeObj("zktree");
        treeNode = treeObj.getNodes()[0];
    }

    var path = treeNode.bakValue;
    var url = "/Home/GetNode?path=" + path;

    $.ajax({
        url: url,
        type: "GET",
        datatype: "Json",
        success: function (data) {
            if (data.businessCode !== -1) {
                var obj = data.returnObj;
                $("#lb-path").text(obj.path);
                $("#lb-version").text(obj.version);
                $("#lb-createtime").text(obj.createTime);
                $("#lb-modifytime").text(obj.modifyTime);
                $("#lb-acl").text(obj.acl);
                $("#lb-value").val(obj.value);
                $("#lb-jsonvalue").val(obj.value);
            } else
                alert(data.Message);
        },
        error: function (data) {
            alert("服务器访问错误");
        }
    });
}

function removeHoverDom(treeId, treeNode) {
    $("#addBtn_" + treeNode.tId).unbind().remove();
};


var newCount = 1;
function addHoverDom(treeId, treeNode) {
    var sObj = $("#" + treeNode.tId + "_span");
    if (treeNode.editNameFlag || $("#addBtn_" + treeNode.tId).length > 0) return;

    var treeNodeVal = treeNode.bakValue;
    var addStr = "<span class='button add' id='addBtn_" + treeNode.tId + "' title='add node' onfocus='this.blur();'></span>";
    sObj.after(addStr);

    var btn = $("#addBtn_" + treeNode.tId);
    if (btn) btn.bind("click", function () {
        $("#edit-path").val(treeNodeVal);
        $("#addModel").modal('show');
        return true;
    });

};