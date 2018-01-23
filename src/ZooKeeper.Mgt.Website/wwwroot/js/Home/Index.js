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

$(document).ready(function () {
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
			alert("服务器访问错误");
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
                $("#lb-value").text(obj.value);
			} else
				alert(data.Message);
		},
		error: function (data) {
			alert("服务器访问错误");
		}
	});
}