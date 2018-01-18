var setting = {
    view: {
        selectedMulti: false
    },
    edit: {
        enable: true,
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

function OnClick(event, treeId, treeNode) {
    var path = treeNode.bakValue;
    var strs = path.split("/");
    if (strs.length === 0 || strs.pop().indexOf("$$") !== 0) return;
    $("#config-address").html(path);
    if (treeNode.open === false) {
        expandNode(treeNode);
    } else {
        var treeObj = $.fn.zTree.getZTreeObj("zktree");
        treeObj.expandNode(treeNode, false, true, true);
    }

    getzknodes(path);
}

function getzknodes(path) {
    var url = "/Config/GetChildNodes?path=" + path;
    $.ajax({
        url: url,
        type: "Get",
        datatype: "Json",
        success: function (data) {
            $("#node-content").html('');
            if (data.businessCode !== -1) {
                showTables(data.returnObj);
            } else {
                alert(data.businessMessage);
            }
        },
        error: function (data) {
            alert("服务器访问错误");
        }
    });
}

function showTables(data) {
    if (data.length > 0) {
        for (var i = 0; i < data.length; i++) {
            randerTable(data[i]);
        }
    }
}

function randerTable(data) {
    var rows = '';
    for (var i = 0; i < data.nodes.length; i++) {
        var node = data.nodes[i];
        var nodePath = data.path + '/' + node.key;
        var isEncrypted = "checked=" + node.isEncryptDisplay == true ? "checked" : "";
        var chxbox = node.isEncryptDisplay === true ? 'checked="checked" disabled="disabled"' : '';

        rows += ' <tr><td>' + node.key + '</td><td>' + node.value + '</td><td>' + node.description +
            '</td><td><input type="checkbox" disabled="disabled" ' + chxbox + ' /></td><td><span name="edit" data-path="' + nodePath + '" class="label label-success option-hover">edit</span>&nbsp;' +
            '<span name= "delete" data-path="' + nodePath + '" class="label label-danger option-hover" >del</span>&nbsp;</td></tr>';
    }

    $("#node-content").append('<div class="box">' +
        '<div class="box-header"><h3 class="box-title">' + data.viewPath + '</h3><span name="delete-all" data-path="' + data.path + '" class="label label-warning option-hover">delete all</span></div>' +
        '<div class="box-body table-responsive no-padding"><table class="table table-hover table-width"><tbody><tr><th>Key</th><th>Value</th><th>Description</th><th>IsEncrypted</th><th>Option</th></tr>' +
        rows + '</tbody></table></div></div>'
    );
}

function add(node, data) {
    var zTree = $.fn.zTree.getZTreeObj("zktree");

    if (node) {
        zTree.removeChildNodes(node);
        zTree.addNodes(node, data);
    }
};

function addnode() {

    var key = $.trim($("#add_key").val());
    var value = $.trim($("#add_Value").val());
    var description = $.trim($("#add_Decription").val());
    var isparent = $("#add-isparent").prop('checked');
    var isencrypted = $("#add-encrypted").prop('checked');

    if (key === "") {
        alert("Key 不能为空");
        return;
    }

    if (key.indexOf("$$") === 0) {
        alert("系统规定，Key 不能以'$$'打头");
        return;
    }

    //if (value === "") {
    //    alert("Value 不能为空");
    //    return;
    //}

    var treeObj = $.fn.zTree.getZTreeObj("zktree");
    var nodes = treeObj.getSelectedNodes();
    var path;
    if (nodes.length === 0) {
        var returnVal = confirm("警告: 没有选择父节点，将默认添加到根节点，是否继续？");
        if (returnVal === false) {
            return;
        }
        path = "/";
    } else {
        path = nodes[0].bakValue;
        var params = path.split("/");
        if (params.pop().indexOf("$$") !== 0) {
            alert("请先选定父节点，再进行节点的添加操作！");
            return;
        }
    }

    var url = "/Config/CreateZNode?path=" + path + "&isParent=" + isparent;
    $.ajax({
        url: url,
        type: "Post",
        datatype: "Json",
        data: {
            "key": key,
            "value": value,
            "description": description,
            "isencryptdisplay": isencrypted
        },
        success: function (data) {
            if (data.returnObj === false) {
                alert(data.businessMessage);
                return;
            } else {
                $("#add_key").val('');
                $("#add_Value").val('');
                $("#add_Decription").val('');
                $("#add-isparent").prop('checked', false);
                $("#add-encrypted").prop('checked', false);

                if (nodes.length > 0) {
                    expandNode(nodes[0]);
                    getzknodes(nodes[0].bakValue);
                } else {
                    expandNode(null);
                }
            }
        },
        error: function (data) {
            alert("服务器访问错误");
        }
    });
}

function exportnodes() {
    var treeObj = $.fn.zTree.getZTreeObj("zktree");
    var nodes = treeObj.getSelectedNodes();
    var path = nodes[0].bakValue;
    var exportUrl = "/Config/Export?path=" + path;
    window.location = exportUrl;
}

function clearChildren(e) {
    var zTree = $.fn.zTree.getZTreeObj("zktree"),
        nodes = zTree.getSelectedNodes(),
        treeNode = nodes[0];
    if (nodes.length == 0 || !nodes[0].isParent) {
        alert("请先选择一个父节点");
        return;
    }
    zTree.removeChildNodes(treeNode);
};

function init() {
    var url = "/Config/GetTreeNodes?path=/";
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
            alert("服务器访问错误");
        }
    });
}

function beforeExpand(treeId, treeNode) {
    expandNode(treeNode);
    return (treeNode.expand !== false);
}

function expandNode(treeNode) {
    if (treeNode == null) {
        var treeObj = $.fn.zTree.getZTreeObj("zktree");
        treeNode = treeObj.getNodes()[0];
    }

    var path = treeNode.bakValue;
    var url = "/Config/GetTreeNodes?path=" + path + "&parentId=" + treeNode.id;

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

function option(event) {
    var opt = $(this).attr("name");
    var path = $(this).data("path");

    if (opt === "delete") {
        deleteopt(path);
    } else if (opt === "delete-all") {
        deleteallopt(path);
    }
    else if (opt === "edit") {
        editoption($(this), path);
    }
}

function deleteopt(path) {
    var returnVal = confirm("确认删除？");
    if (returnVal === false) {
        return;
    } else {
        var url = "/Config/DeleteZNode?path=" + path;
        $.ajax({
            url: url,
            type: "Get",
            success: function (data) {
                if (data.returnObj === false) {
                    alert(data.businessMessage);
                } else {
                    var treeObj = $.fn.zTree.getZTreeObj("zktree");
                    var nodes = treeObj.getSelectedNodes();
                    if (nodes.length > 0) {
                        expandNode(nodes[0]);
                        getzknodes(nodes[0].bakValue);
                    }
                }
            },
            error: function (data) {
                alert("服务器访问错误");
            }
        });
    }
}

function deleteallopt(path) {
    var returnVal = confirm("此操作会删除该节点以及所有的子节点,确认删除？");
    if (returnVal === false) {
        return;
    } else {
        var url = "/Config/DeleteRecursiveZNode?path=" + path;
        $.ajax({
            url: url,
            type: "Get",
            success: function (data) {
                if (data.returnObj === false) {
                    alert(data.businessMessage);
                } else {
                    var treeObj = $.fn.zTree.getZTreeObj("zktree");
                    var nodes = treeObj.getSelectedNodes();
                    if (nodes.length > 0) {
                        var parentNode = nodes[0].getParentNode();
                        $("#config-address").html("节点配置地址：" + parentNode.bakValue);
                        treeObj.selectNode(parentNode);
                        expandNode(parentNode);
                        getzknodes(parentNode.bakValue);
                    }
                }
            },
            error: function (data) {
                alert("服务器访问错误");
            }
        });
    }
}

function saveopt() {
    var path = $("#edit-path").val();
    var key = $("#edit-key").html();
    var value = $.trim($("#edit-value").val());
    var description = $.trim($("#edit-desc").val());
    var isencryptdisplay = $("#edit-encrypt").prop('checked');

    //if (value === "") {
    //    alert("Value 不能为空");
    //    return;
    //}

    $.ajax({
        url: "/Config/UpdateZNode?path=" + path,
        type: "Post",
        datatype: "Json",
        data: {
            "key": key,
            "value": value,
            "description": description,
            "isencryptdisplay": isencryptdisplay
        },
        success: function (data) {
            if (data.returnObj === true) {
                $("#edit-value").val("");
                $("#edit-desc").val("");
                $("#edit-encrypt").prop("checked", false);
                $("#edit-path").val("");

                $("#editModel").modal('hide');
                $("#btnEditModel").button('reset');

                var treeObj = $.fn.zTree.getZTreeObj("zktree");
                var nodes = treeObj.getSelectedNodes();
                if (nodes.length > 0) {
                    expandNode(nodes[0]);
                    getzknodes(nodes[0].bakValue);
                }
            }
            else {
                alert(data.businessMessage);
            }
        },
        error: function (data) {
            alert("服务器访问错误");
        }
    })
}

function editoption(sender, path) {
    $.ajax({
        url: "/Config/GetNode?path=" + path,
        type: "Get",
        success: function (data) {
            if (data.businessCode !== -1) {
                var obj = data.returnObj;

                $("#edit-key").html(obj.key);

                var val = obj.isEncryptDisplay ? "" : obj.value;
                var displayVal;
                if (val != null) {
                    try {
                        displayVal = JSON.stringify($.parseJSON(val), null, 4);
                    } catch (e) {
                        displayVal = val;
                    }
                }

                $("#edit-value").val(displayVal);

                $("#edit-desc").val(obj.description);
                $("#edit-encrypt").prop("checked", obj.isEncryptDisplay);

                if (!obj.isEncryptDisplay)
                    $("#edit-encrypt").prop("disabled", "");
                else
                    $("#edit-encrypt").prop("disabled", "disabled");

                $("#edit-path").val(path);

                $("#btnEditModel").button('reset');
                $("#editModel").modal();
            }
            else {
                alert(data.businessMessage);
            }
        },
        error: function () {
            alert("服务器访问错误");
        }
    });
}

function isJsonFormat(str) {
    try {
        $.parseJSON(str);
    } catch (e) {
        return false;
    }
    return true;
}

$(document).ready(function () {
    init();
    $("#submit_add").bind("click", addnode);

    $("#submit_export").bind("click", exportnodes);

    $("#btnEditModel").bind("click", saveopt);

    $("#submit_import").fileupload({
        url: "/Config/Import",
        dataType: "json",
        add: function (e, data) {
            var nodes = $.fn.zTree.getZTreeObj("zktree").getSelectedNodes();
            if (nodes.length === 0) {
                alert("请先选定上传对应的父节点！");
                return;
            }
            var path = nodes[0].bakValue;
            data.formData = { path: path };

            if (data.files.length !== 1) {
                alert("一次只能上传一个文件");
                return;
            }

            if (confirm("为了防止上传误操作，当前限制只能针对某一个父节点进行操作，并且上传文件会覆盖原有所有节点配置,您确认上传吗？")) {
                data.submit();
            } else
                return;
        },
        done: function (e, data) {
            if (data.result.returnObj == true) {
                alert("导入成功");
                var nodes = $.fn.zTree.getZTreeObj("zktree").getSelectedNodes();
                expandNode(nodes[0]);
                getzknodes(nodes[0].bakValue);
            } else {
                alert("导入失败：" + data.result.businessMessage);
            }
        },
        fail: function (event, data) {
            alert("上传文件错误！");
        }
    });

    $("#node-content").delegate("div span", "click", option);

    $("#add-encrypted").bind("click", function (e) {
        if (e.currentTarget.checked) {
            $("#add-isparent").prop("disabled", "disabled");
        } else {
            $("#add-isparent").prop("disabled", "");
        }
    });

    $("#add-isparent").bind("click", function (e) {
        if (e.currentTarget.checked) {
            $("#add-encrypted").prop("checked", false)
            $("#add-encrypted").prop("disabled", "disabled");
        } else {
            $("#add-encrypted").prop("disabled", "");
        }
    });
});