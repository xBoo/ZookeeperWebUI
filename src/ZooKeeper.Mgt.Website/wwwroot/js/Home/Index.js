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
}

function OnClick(event, treeId, treeNode) {
	var path = treeNode.bakValue;
	$("#config-address").html(path);
	if (treeNode.open === false) {
		expandNode(treeNode);
	} else {
		var treeObj = $.fn.zTree.getZTreeObj("zktree");
		treeObj.expandNode(treeNode, false, true, true);
	}
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